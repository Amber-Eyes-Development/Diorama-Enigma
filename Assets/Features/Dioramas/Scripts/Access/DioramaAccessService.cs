using System;
using System.Collections.Generic;
using DioramaEnigma.Sequences;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Сервис доступа: вычисляет открытые/пройденные диорамы по графу входящих связей
    /// </summary>
    /// <remarks>
    /// Ассет-модель: инициализируется лениво при первом запросе (как InMemoryData), живёт сквозь сцены,
    /// ссылается напрямую (без бутстраппера/канала). Подписки на шаги-условия — лениво; очистка в OnDisable
    /// </remarks>
    [CreateAssetMenu(menuName = "Dioramas/Access Service", fileName = nameof(DioramaAccessService))]
    public sealed class DioramaAccessService : ScriptableObject
    {
        #region События
        /// <summary> Диорама открыта </summary>
        public event Action<DioramaDefinition> onDioramaUnlocked;
        /// <summary> Диорама пройдена впервые </summary>
        public event Action<DioramaDefinition> onDioramaCompleted;
        /// <summary> Блок стал виден (появилась первая открытая диорама) </summary>
        public event Action<DioramaBlock> onBlockUnlocked;
        /// <summary> Блок пройден целиком (все его диорамы пройдены) — впервые </summary>
        public event Action<DioramaBlock> onBlockCompleted;
        #endregion

        #region Параметры и переменные
        
        [Tooltip("Реестр диорам")]
        [SerializeField] private DioramaRegistry registry;

        private readonly HashSet<string> unlocked = new();
        private readonly HashSet<string> everCompleted = new();
        private readonly HashSet<string> visibleBlocks = new();
        private readonly HashSet<string> completedBlocks = new();
        private readonly List<SequenceStep> subscribedSteps = new();

        private bool initialized;
        
        #endregion

        #region Жизненный цикл

        // Построение графа из персистентного состояния и подписка на шаги-условия — лениво при первом запросе
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
            SeedEverCompleted();
            SeedUnlockedFromEverCompleted();
            SubscribeConditionSteps();
            EvaluateUnlocks(false);
            SeedVisibleBlocks();
            SeedCompletedBlocks();
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
            unlocked.Clear();
            everCompleted.Clear();
            visibleBlocks.Clear();
            completedBlocks.Clear();
            initialized = false;
        }

        #endregion

        #region Запросы и состояния

        /// <summary> Открыта ли диорама </summary>
        public bool IsUnlocked(DioramaDefinition def)
        {
            EnsureInitialized();
            return def != null && unlocked.Contains(def.Id);
        }

        /// <summary> Пройдена ли диорама прямо сейчас (с учётом отката шагов) </summary>
        public bool IsCompleted(DioramaDefinition def) => def != null && def.IsCompleted;

        /// <summary> Состояние доступа диорамы </summary>
        public DioramaState StateOf(DioramaDefinition def)
        {
            EnsureInitialized();
            if (def == null) return DioramaState.Locked;
            if (def.IsCompleted) return DioramaState.Completed;
            if (unlocked.Contains(def.Id)) return DioramaState.Unlocked;

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
            if (def == null) return;
            if (!everCompleted.Add(def.Id)) return;

            unlocked.Add(def.Id);
            PersistCompleted();
            onDioramaCompleted?.Invoke(def);
            EvaluateUnlocks(true);

            TryAnnounceBlockCompleted(def);
        }

        // Завершение этой диорамы могло добрать последний кусок блока — сообщить о завершении блока однократно
        private void TryAnnounceBlockCompleted(DioramaDefinition def)
        {
            var block = registry.BlockOf(def);
            if (block == null || completedBlocks.Contains(block.Id)) return;
            if (!IsBlockCompleted(block)) return;

            completedBlocks.Add(block.Id);
            onBlockCompleted?.Invoke(block);
        }

        #endregion

        #region UI API

        /// <summary> Блоки с хотя бы одной открытой диорамой, в порядке реестра </summary>
        public IReadOnlyList<DioramaBlock> VisibleBlocks()
        {
            EnsureInitialized();
            var result = new List<DioramaBlock>();

            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry?.Block == null) continue;
                if (HasUnlockedDiorama(blockEntry)) result.Add(blockEntry.Block);
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
                    if (def == null || !unlocked.Contains(def.Id)) continue;
                    result.Add(new DioramaNode(def, StateOf(def)));
                }
            }

            return result;
        }

        /// <summary> Сколько блоков ещё скрыто (нет ни одной открытой диорамы) </summary>
        public int HiddenBlockCount()
        {
            EnsureInitialized();
            int hidden = 0;

            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry?.Block == null) continue;
                if (!HasUnlockedDiorama(blockEntry)) hidden++;
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
                    if (def != null && !unlocked.Contains(def.Id)) hidden++;
                }
            }

            return hidden;
        }

        /// <summary> Последняя по порядку реестра открытая диорама — фронт прогресса (или null) </summary>
        public DioramaDefinition LastUnlockedDiorama()
        {
            EnsureInitialized();
            DioramaDefinition last = null;

            foreach (var entry in registry.Entries())
            {
                var def = entry.Definition;
                if (def != null && unlocked.Contains(def.Id)) last = def;
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

        #region Evaluation

        private void SeedEverCompleted()
        {
            foreach (var entry in registry.Entries())
            {
                var def = entry.Definition;
                if (def != null && def.IsCompleted) everCompleted.Add(def.Id);
            }

            foreach (var id in DioramaProgressStore.LoadCompleted())
                if (!string.IsNullOrEmpty(id)) everCompleted.Add(id);
        }

        // Пройденная диорама обязательно была открыта — открытость монотонна и не должна откатываться
        private void SeedUnlockedFromEverCompleted()
        {
            foreach (var id in everCompleted)
                unlocked.Add(id);
        }

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

        // Транзитивно открыть все достижимые диорамы; при notify — события на новые
        private void EvaluateUnlocks(bool notify)
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                foreach (var entry in registry.Entries())
                {
                    var def = entry.Definition;
                    if (def == null || unlocked.Contains(def.Id)) continue;
                    if (!ShouldUnlock(entry)) continue;

                    unlocked.Add(def.Id);
                    changed = true;

                    var block = registry.BlockOf(def);
                    bool blockNewlyVisible = block != null && visibleBlocks.Add(block.Id);

                    if (notify)
                    {
                        onDioramaUnlocked?.Invoke(def);
                        if (blockNewlyVisible) onBlockUnlocked?.Invoke(block);
                    }
                }
            }
        }

        private bool ShouldUnlock(DioramaEntry entry)
        {
            if (entry.UnlockedFromStart) return true;

            foreach (var link in entry.IncomingLinks)
                if (IsLinkSatisfied(link)) return true;

            return false;
        }

        private bool IsLinkSatisfied(DioramaLink link)
        {
            if (link == null) return false;

            switch (link.Condition)
            {
                case DioramaLinkCondition.SourceUnlocked:
                    return link.Source != null && unlocked.Contains(link.Source.Id);

                case DioramaLinkCondition.SourceCompleted:
                    return link.Source != null && everCompleted.Contains(link.Source.Id);

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

        private void SeedVisibleBlocks()
        {
            foreach (var entry in registry.Entries())
            {
                var def = entry.Definition;
                if (def == null || !unlocked.Contains(def.Id)) continue;

                var block = registry.BlockOf(def);
                if (block != null) visibleBlocks.Add(block.Id);
            }
        }

        // Блоки, уже пройденные на момент инициализации, не должны заново стрелять onBlockCompleted в этой сессии
        private void SeedCompletedBlocks()
        {
            foreach (var blockEntry in registry.Blocks)
            {
                if (blockEntry?.Block == null) continue;
                if (IsBlockEntryCompleted(blockEntry)) completedBlocks.Add(blockEntry.Block.Id);
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

        // Блок пройден, если у него есть диорамы и все они «пройдены когда-либо» (монотонно)
        private bool IsBlockEntryCompleted(DioramaBlockEntry blockEntry)
        {
            bool any = false;

            foreach (var entry in blockEntry.Dioramas)
            {
                var def = entry?.Definition;
                if (def == null) continue;

                any = true;
                if (!everCompleted.Contains(def.Id)) return false;
            }

            return any;
        }

        #endregion

        #region Helpers

        private bool HasUnlockedDiorama(DioramaBlockEntry blockEntry)
        {
            foreach (var entry in blockEntry.Dioramas)
            {
                var def = entry?.Definition;
                if (def != null && unlocked.Contains(def.Id)) return true;
            }

            return false;
        }

        private void PersistCompleted() => DioramaProgressStore.SaveCompleted(everCompleted);

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
