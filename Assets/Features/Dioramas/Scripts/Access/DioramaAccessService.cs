using System;
using System.Collections.Generic;
using DioramaEnigma.Sequences;
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
        private readonly List<SequenceStep> subscribedSteps = new();

        private bool initialized;

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

        /// <summary> Пройдена ли диорама прямо сейчас (с учётом отката шагов) </summary>
        public bool IsCompleted(DioramaDefinition def) => def != null && def.IsCompleted;

        /// <summary> Состояние доступа диорамы </summary>
        public DioramaState StateOf(DioramaDefinition def)
        {
            EnsureInitialized();
            if (def == null) return DioramaState.Locked;
            if (def.IsCompleted) return DioramaState.Completed;
            if (def.IsUnlocked) return DioramaState.Unlocked;

            return DioramaState.Locked;
        }

        /// <summary> Определение по идентификатору (или null) </summary>
        /// <param name="id">Идентификатор диорамы</param>
        public DioramaDefinition ById(string id) => registry != null ? registry.ById(id) : null;

        /// <summary> Блок по идентификатору (или null) </summary>
        /// <param name="id">Идентификатор блока</param>
        public DioramaBlock BlockById(string id)
        {
            if (registry == null || string.IsNullOrEmpty(id)) return null;

            foreach (var blockEntry in registry.Blocks)
                if (blockEntry?.Block != null && blockEntry.Block.Id == id) return blockEntry.Block;

            return null;
        }

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
            if (def == null || !def.IsCompleted) return;

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

            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry?.Block != block) continue;

                DioramaDefinition entryDiorama = null;

                foreach (var entry in blockEntry.Dioramas)
                {
                    var def = entry?.Definition;
                    if (def == null) continue;

                    ResetSteps(def, includeGlobal: false);
                    def.LockReset();
                    entryDiorama ??= def;
                }

                entryDiorama?.Unlock();

                break;
            }

            announcedBlocks.Remove(block.Id);
            EvaluateUnlocks(false);
            onBlockRestarted?.Invoke(block);
        }

        /// <summary>
        /// Сбросить весь прогресс (новая игра): все шаги, разблокировки диорам/блоков, выбор
        /// </summary>
        public void ResetAllProgress()
        {
            EnsureInitialized();
            if (registry == null) return;

            foreach (var entry in registry.Entries())
            {
                var def = entry.Definition;
                if (def == null) continue;

                ResetSteps(def, includeGlobal: true);
                def.LockReset();
            }

            foreach (var blockEntry in registry.Blocks)
                blockEntry?.Block?.LockReset();

            DioramaProgressStore.SaveLastActive(null);
            DioramaProgressStore.SaveSelectedBlock(null);

            announcedBlocks.Clear();
            EvaluateUnlocks(false); // заново открыть стартовые диорамы/блоки
            SeedAnnouncedBlocks();
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
                    result.Add(new DioramaNode(def, StateOf(def)));
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

        /// <summary> Сколько диорам блока пройдено (живое состояние) </summary>
        /// <param name="block">Блок</param>
        public int CompletedCountInBlock(DioramaBlock block)
        {
            int completed = 0;
            foreach (var def in AllInBlock(block))
                if (def != null && def.IsCompleted) completed++;

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

        #region API карты блока

        /// <summary> Построить срез карты: все диорамы как узлы (с состоянием) и связи как рёбра </summary>
        public DioramaMapView BuildMap()
        {
            EnsureInitialized();
            var nodes = new List<DioramaNode>();
            var edges = new List<DioramaEdge>();

            foreach (var entry in registry.Entries())
            {
                var def = entry.Definition;
                if (def == null) continue;

                nodes.Add(new DioramaNode(def, StateOf(def)));

                foreach (var link in entry.IncomingLinks)
                {
                    if (link?.Source == null) continue;
                    edges.Add(new DioramaEdge(link.Source, def, link.Condition));
                }
            }

            return new DioramaMapView(nodes, edges);
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

        private void OnConditionStepChanged(bool _) => EvaluateUnlocks(true);

        // Транзитивно выставить флаг IsUnlocked достижимым диорамам/блокам; при notify — события на новые
        private void EvaluateUnlocks(bool notify)
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                foreach (var entry in registry.Entries())
                {
                    var def = entry.Definition;
                    if (def == null || def.IsUnlocked) continue;
                    if (!ShouldUnlock(entry)) continue;

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

        private bool ShouldUnlock(DioramaEntry entry)
        {
            if (IsFirstEntry(entry)) return true;

            foreach (var link in entry.IncomingLinks)
                if (IsLinkSatisfied(link)) return true;

            return false;
        }

        // Самая первая диорама реестра — единственная стартовая точка (доступна с самого начала)
        private bool IsFirstEntry(DioramaEntry entry)
        {
            var entries = registry.Entries();
            return entries.Count > 0 && ReferenceEquals(entries[0], entry);
        }

        private bool IsLinkSatisfied(DioramaLink link)
        {
            if (link == null) return false;

            switch (link.Condition)
            {
                case DioramaLinkCondition.SourceUnlocked:
                    return link.Source != null && link.Source.IsUnlocked;

                case DioramaLinkCondition.SourceCompleted:
                    // Момент первого выполнения фиксируется персистентным флагом цели — откат источника цель не закроет
                    return link.Source != null && link.Source.IsCompleted;

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

        private bool IsBlockCompleted(DioramaBlock block)
        {
            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry?.Block != block) continue;
                return IsBlockEntryCompleted(blockEntry);
            }

            return false;
        }

        // Блок пройден, если у него есть диорамы и все они пройдены прямо сейчас (живое состояние)
        private bool IsBlockEntryCompleted(DioramaBlockEntry blockEntry)
        {
            bool any = false;

            foreach (var entry in blockEntry.Dioramas)
            {
                var def = entry?.Definition;
                if (def == null) continue;

                any = true;
                if (!def.IsCompleted) return false;
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
