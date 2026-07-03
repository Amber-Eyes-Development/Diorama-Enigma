using System;
using System.Collections.Generic;
using DioramaEnigma.Sequences;
using Extensions.Helpers.Enumerations;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Сервис доступа: координирует разблокировку диорам/блоков по графу входящих связей
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Access Service", fileName = nameof(DioramaAccessService))]
    public sealed class DioramaAccessService : ScriptableObject
    {
        #region События
        /// <summary> Диорама открыта </summary>
        public event Action<DioramaDefinition> onDioramaUnlocked;
        /// <summary> Диорама пройдена </summary>
        public event Action<DioramaDefinition> onDioramaCompleted;
        /// <summary> Блок стал доступен (открылась первая диорама) </summary>
        public event Action<DioramaBlock> onBlockUnlocked;
        /// <summary> Блок пройден целиком (все его диорамы пройдены) </summary>
        public event Action<DioramaBlock> onBlockCompleted;
        /// <summary> Блок перезапущен из меню (шаги и внутриблоковая разблокировка сброшены) </summary>
        public event Action<DioramaBlock> onBlockRestarted;
        /// <summary> Сброшен весь прогресс (новая игра) </summary>
        public event Action onProgressReset;
        #endregion

        #region Параметры и переменные

        [Tooltip("Реестр диорам")]
        [SerializeField] private DioramaRegistry registry;

        // Транзиентная защита от повторного onBlockCompleted в пределах сессии
        private readonly HashSet<string> announcedBlocks = new();
        // То же для onDioramaCompleted: уже решённые на момент инициализации диорамы не стреляют событием заново
        private readonly HashSet<string> announcedCompletions = new();
        private readonly List<SequenceStep> subscribedSteps = new();

        private bool initialized;
        // На время массового сброса шагов глушим реакцию на их события — пересчёт делаем один раз в конце
        private bool suppressStepReeval;

        #endregion

        #region Жизненный цикл

        // Подписка на шаги-условия и распространение разблокировок по флагам — лениво при первом запросе
        private void EnsureInitialized()
        {
            if (initialized) return;

            if (registry == null)
            {
                ServiceDebug.LogError($"Реестр диорам {nameof(registry)} не назначен");
                return;
            }

            initialized = true;

            ValidateLinks();
            SubscribeConditionSteps();
            EvaluateUnlocks(false);
            SeedAnnouncedBlocks();
            SeedAnnouncedCompletions();
        }

        private void OnDisable()
        {
            foreach (var step in subscribedSteps)
            {
                if (step == null) continue;

                step.onCompletionChanged -= OnConditionStepChanged;
                step.onUnlockChanged -= OnConditionStepChanged;
            }

            subscribedSteps.Clear();
            announcedBlocks.Clear();
            announcedCompletions.Clear();
            initialized = false;
        }

        #endregion

        #region Запросы и состояния

        /// <summary> Открыта ли диорама </summary>
        public bool IsUnlocked(DioramaDefinition def)
        {
            EnsureInitialized();
            return def != null && def.IsUnlocked;
        }

        /// <summary> Решена ли диорама — трек №2 (все линейные группы пройдены), для прогрессии </summary>
        public bool IsSolved(DioramaDefinition def) => def != null && def.IsSolved;

        /// <summary> Полностью ли пройдены все шаги — трек №1 (вкл. фоновые), для UI-индикации </summary>
        public bool IsFullyCompleted(DioramaDefinition def) => def != null && def.IsFullyCompleted;

        /// <summary> Состояние доступа диорамы (Completed = решена, трек №2) </summary>
        public DioramaState StateOf(DioramaDefinition def)
        {
            EnsureInitialized();
            if (def == null) return DioramaState.Locked;
            if (def.IsSolved) return DioramaState.Completed;
            if (def.IsUnlocked) return DioramaState.Unlocked;

            return DioramaState.Locked;
        }

        /// <summary> Определение по идентификатору (или null) </summary>
        /// <param name="id">Идентификатор диорамы</param>
        public DioramaDefinition ById(string id) => registry != null ? registry.ById(id) : null;

        /// <summary> Блок по идентификатору (или null) </summary>
        /// <param name="id">Идентификатор блока</param>
        public DioramaBlock BlockById(string id) => registry != null ? registry.BlockById(id) : null;

        /// <summary> Блок, которому принадлежит диорама (или null) </summary>
        /// <param name="def">Определение диорамы</param>
        public DioramaBlock BlockOf(DioramaDefinition def) => registry != null ? registry.BlockOf(def) : null;

        /// <summary> Следующий блок в порядке реестра после указанного (или null, если последний/не найден) </summary>
        /// <param name="block">Текущий блок</param>
        public DioramaBlock NextBlock(DioramaBlock block)
        {
            if (registry == null || block == null) return null;

            bool found = false;
            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry?.Block == null) continue;
                if (found) return blockEntry.Block;
                if (blockEntry.Block == block) found = true;
            }

            return null;
        }

        #endregion

        #region Прогресс пользователя

        /// <summary>
        /// Отметить диораму пройденной (вызывается при завершении её раннера) и каскадно переоценить доступ
        /// </summary>
        /// <param name="def">Пройденная диорама</param>
        public void MarkCompleted(DioramaDefinition def)
        {
            EnsureInitialized();
            if (def == null || !def.IsSolved) return;

            // Уже учтено (в т.ч. загрузка уже решённой диорамы на сцене) — не дублируем событие и каскад
            if (!announcedCompletions.Add(def.Id)) return;

            onDioramaCompleted?.Invoke(def);
            EvaluateUnlocks(true);
            TryAnnounceBlockCompleted(def);
        }

        /// <summary>
        /// Перезапустить блок «как впервые»: сбросить шаги его диорам и внутриблоковую разблокировку,
        /// сохранив разблокировку самого блока и весь последующий прогресс
        /// </summary>
        /// <param name="block">Блок</param>
        public void RestartBlock(DioramaBlock block)
        {
            EnsureInitialized();
            if (registry == null || block == null) return;

            suppressStepReeval = true;
            try
            {
                foreach (var blockEntry in registry.Blocks)
                {
                    if (blockEntry?.Block != block) continue;

                    foreach (var entry in blockEntry.Dioramas)
                    {
                        var def = entry?.Definition;
                        if (def == null) continue;

                        ResetSteps(def, includeGlobal: false);
                        def.LockReset();
                        announcedCompletions.Remove(def.Id);
                    }

                    break;
                }
            }
            finally { suppressStepReeval = false; }

            announcedBlocks.Remove(block.Id);
            EvaluateUnlocks(false); // вход блока заново откроет правило первой записи / неявная связь по графу
            onBlockRestarted?.Invoke(block);
        }

        /// <summary>
        /// Сбросить весь прогресс (новая игра): все шаги, разблокировки диорам/блоков, выбор
        /// </summary>
        public void ResetAllProgress()
        {
            EnsureInitialized();
            if (registry == null) return;

            suppressStepReeval = true;
            try
            {
                foreach (var entry in registry.Entries())
                {
                    var def = entry.Definition;
                    if (def == null) continue;

                    ResetSteps(def, includeGlobal: true);
                    def.LockReset();
                }

                foreach (var blockEntry in registry.Blocks)
                    blockEntry?.Block?.LockReset();
            }
            finally { suppressStepReeval = false; }

            DioramaProgressStore.SaveLastActive(null);
            DioramaProgressStore.SaveSelectedBlock(null);

            announcedBlocks.Clear();
            announcedCompletions.Clear();
            EvaluateUnlocks(false); // заново открыть стартовые диорамы/блоки
            SeedAnnouncedBlocks();
            SeedAnnouncedCompletions();
            onProgressReset?.Invoke();
        }

        // Завершение этой диорамы могло добрать последний кусок блока — сообщить о завершении блока однократно
        private void TryAnnounceBlockCompleted(DioramaDefinition def)
        {
            var block = registry.BlockOf(def);
            if (block == null || announcedBlocks.Contains(block.Id)) return;
            if (!IsBlockCompleted(block)) return;

            announcedBlocks.Add(block.Id);
            onBlockCompleted?.Invoke(block);
        }

        #endregion

        #region UI API

        /// <summary> Разблокированные блоки в порядке реестра </summary>
        public IReadOnlyList<DioramaBlock> VisibleBlocks()
        {
            EnsureInitialized();
            var result = new List<DioramaBlock>();

            foreach (var blockEntry in registry.Blocks)
            {
                var block = blockEntry?.Block;
                if (block != null && block.IsUnlocked) result.Add(block);
            }

            return result;
        }

        /// <summary> Открытые диорамы блока с их состоянием (закрытые скрыты), в порядке блока </summary>
        /// <param name="block">Блок</param>
        public IReadOnlyList<DioramaNode> DioramasInBlock(DioramaBlock block)
        {
            EnsureInitialized();
            var result = new List<DioramaNode>();

            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry == null || blockEntry.Block != block) continue;

                foreach (var entry in blockEntry.Dioramas)
                {
                    var def = entry?.Definition;
                    if (def == null || !def.IsUnlocked) continue;
                    result.Add(new DioramaNode(def, StateOf(def), def.IsFullyCompleted));
                }
            }

            return result;
        }

        /// <summary> Сколько блоков ещё закрыто </summary>
        public int HiddenBlockCount()
        {
            EnsureInitialized();
            int hidden = 0;

            foreach (var blockEntry in registry.Blocks)
            {
                var block = blockEntry?.Block;
                if (block != null && !block.IsUnlocked) hidden++;
            }

            return hidden;
        }

        /// <summary> Сколько диорам блока ещё закрыто </summary>
        /// <param name="block">Блок</param>
        public int HiddenDioramaCount(DioramaBlock block)
        {
            EnsureInitialized();
            int hidden = 0;

            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry == null || blockEntry.Block != block) continue;

                foreach (var entry in blockEntry.Dioramas)
                {
                    var def = entry?.Definition;
                    if (def != null && !def.IsUnlocked) hidden++;
                }
            }

            return hidden;
        }

        /// <summary> Сколько диорам блока решено (живое состояние, трек №2) </summary>
        /// <param name="block">Блок</param>
        public int CompletedCountInBlock(DioramaBlock block)
        {
            int completed = 0;
            foreach (var def in AllInBlock(block))
                if (def != null && def.IsSolved) completed++;

            return completed;
        }

        /// <summary> Последняя по порядку реестра открытая диорама — фронт прогресса (или null) </summary>
        public DioramaDefinition LastUnlockedDiorama()
        {
            EnsureInitialized();
            DioramaDefinition last = null;

            foreach (var entry in registry.Entries())
            {
                var def = entry.Definition;
                if (def != null && def.IsUnlocked) last = def;
            }

            return last;
        }

        /// <summary> ВСЕ диорамы блока в порядке реестра (вкл. закрытые) — для загрузки сессии блока </summary>
        /// <param name="block">Блок</param>
        public IReadOnlyList<DioramaDefinition> AllInBlock(DioramaBlock block)
        {
            var result = new List<DioramaDefinition>();
            if (registry == null) return result;

            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry == null || blockEntry.Block != block) continue;

                foreach (var entry in blockEntry.Dioramas)
                    if (entry?.Definition != null) result.Add(entry.Definition);
            }

            return result;
        }

        #endregion

        #region Разблокировка

        private void SubscribeConditionSteps()
        {
            var seen = new HashSet<SequenceStep>();

            foreach (var entry in registry.Entries())
            {
                foreach (var link in entry.IncomingLinks)
                {
                    if (link == null || link.Condition != DioramaLinkCondition.StepTrigger) continue;

                    var step = link.Step;
                    if (step == null || !seen.Add(step)) continue;

                    step.onCompletionChanged += OnConditionStepChanged;
                    step.onUnlockChanged += OnConditionStepChanged;
                    subscribedSteps.Add(step);
                }
            }
        }

        private void OnConditionStepChanged(bool _)
        {
            if (suppressStepReeval) return; // массовый сброс сам вызовет один пересчёт в конце
            EvaluateUnlocks(true);
        }

        // Транзитивно выставить флаг IsUnlocked достижимым диорамам/блокам; при notify — события на новые
        // Разблокировка монотонна: однажды открытая диорама остаётся открытой даже при откате условий
        private void EvaluateUnlocks(bool notify)
        {
            var entries = registry.Entries();
            bool changed = true;

            while (changed)
            {
                changed = false;

                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var def = entry.Definition;
                    if (def == null || def.IsUnlocked) continue;
                    if (!ShouldUnlock(entry, i)) continue;

                    def.Unlock();
                    changed = true;

                    var block = registry.BlockOf(def);
                    bool blockWasLocked = block != null && !block.IsUnlocked;
                    block?.Unlock();

                    if (notify)
                    {
                        onDioramaUnlocked?.Invoke(def);
                        if (blockWasLocked) onBlockUnlocked?.Invoke(block);
                    }
                }
            }
        }

        // Условие открытия: первая запись реестра — стартовая; без связей — по решению предыдущей записи;
        // со связями — их комбинация логическим оператором записи
        private bool ShouldUnlock(DioramaEntry entry, int index)
        {
            if (index == 0) return true;

            var links = entry.IncomingLinks;
            if (links == null || links.Count == 0)
                return PreviousSolved(index);

            if (entry.LinkOperator == LogicOperator.And)
            {
                foreach (var link in links)
                    if (!IsLinkSatisfied(link)) return false;

                return true;
            }

            foreach (var link in links)
                if (IsLinkSatisfied(link)) return true;

            return false;
        }

        // Неявная линейная связь: диорама без входящих связей открывается, когда решена предыдущая запись реестра
        private bool PreviousSolved(int index)
        {
            var prev = registry.Entries()[index - 1].Definition;
            return prev != null && prev.IsSolved;
        }

        private bool IsLinkSatisfied(DioramaLink link)
        {
            if (link == null) return false;

            switch (link.Condition)
            {
                case DioramaLinkCondition.SourceUnlocked:
                    return link.Source != null && link.Source.IsUnlocked;

                case DioramaLinkCondition.SourceCompleted:
                    // «Решена» = трек №2 (линейные группы). Открытая диорама монотонна — откат источника её не закроет
                    return link.Source != null && link.Source.IsSolved;

                case DioramaLinkCondition.StepTrigger:
                    return IsStepTriggerSatisfied(link);

                default:
                    ServiceDebug.LogError($"Необработанное условие связи: {link.Condition}");
                    return false;
            }
        }

        private static bool IsStepTriggerSatisfied(DioramaLink link)
        {
            var step = link.Step;
            if (step == null) return false;

            var trigger = link.StepTrigger;
            if (trigger.IsRejection() || trigger.IsChange()) return false;

            return trigger.IsSatisfiedBy(step.IsCompleted, step.IsUnlocked);
        }

        // Блоки, уже пройденные на момент инициализации, не должны заново стрелять onBlockCompleted в этой сессии
        private void SeedAnnouncedBlocks()
        {
            foreach (var blockEntry in registry.Blocks)
            {
                var block = blockEntry?.Block;
                if (block != null && IsBlockCompleted(block)) announcedBlocks.Add(block.Id);
            }
        }

        // Диорамы, уже решённые на момент инициализации, не должны заново стрелять onDioramaCompleted в этой сессии
        private void SeedAnnouncedCompletions()
        {
            foreach (var entry in registry.Entries())
            {
                var def = entry.Definition;
                if (def != null && def.IsSolved) announcedCompletions.Add(def.Id);
            }
        }

        private bool IsBlockCompleted(DioramaBlock block)
        {
            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry?.Block != block) continue;
                return IsBlockEntryCompleted(blockEntry);
            }

            return false;
        }

        // Блок пройден, если у него есть диорамы и все они решены прямо сейчас (живое состояние, трек №2)
        private bool IsBlockEntryCompleted(DioramaBlockEntry blockEntry)
        {
            bool any = false;

            foreach (var entry in blockEntry.Dioramas)
            {
                var def = entry?.Definition;
                if (def == null) continue;

                any = true;
                if (!def.IsSolved) return false;
            }

            return any;
        }

        // Сброс шагов последовательности диорамы
        private static void ResetSteps(DioramaDefinition def, bool includeGlobal)
        {
            var seq = def.Sequence;
            if (seq == null) return;

            foreach (var stepEntry in seq.Steps)
            {
                var step = stepEntry?.Step;
                if (step == null) continue;
                if (!includeGlobal && step is SequenceStep leaf && leaf.IsGlobal) continue;

                step.ResetState();
            }
        }

        /// <summary> Предупредить о некорректной конфигурации связей </summary>
        private void ValidateLinks()
        {
            foreach (var entry in registry.Entries())
            {
                var def = entry.Definition;

                foreach (var link in entry.IncomingLinks)
                {
                    if (link == null) continue;

                    if (link.Source == null)
                        ServiceDebug.LogWarning($"Связь диорамы {def.name}: источник не назначен");

                    if (link.Condition != DioramaLinkCondition.StepTrigger) continue;

                    if (link.Step == null)
                        ServiceDebug.LogWarning($"Связь диорамы {def.name}: шаг-условие не назначен");
                    else if (link.StepTrigger.IsRejection() || link.StepTrigger.IsChange())
                        ServiceDebug.LogError($"Связь диорамы {def.name}: триггер {link.StepTrigger} не задаёт состояние — связь не откроется");
                }
            }
        }

        #endregion
    }
}
