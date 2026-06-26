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
        /// <summary> Диорама открыта </summary>
        public event Action<DioramaDefinition> onDioramaUnlocked;
        /// <summary> Диорама пройдена впервые </summary>
        public event Action<DioramaDefinition> onDioramaCompleted;
        /// <summary> Блок стал виден (появилась первая открытая диорама) </summary>
        public event Action<DioramaBlock> onBlockUnlocked;

        [Tooltip("Реестр диорам")]
        [SerializeField] private DioramaRegistry registry;

        private readonly HashSet<string> unlocked = new();
        private readonly HashSet<string> everCompleted = new();
        private readonly HashSet<string> visibleBlocks = new();
        private readonly List<SequenceStep> subscribedSteps = new();

        private bool initialized;

        #region Lifecycle

        // Построение графа из персистентного состояния и подписка на шаги-условия — лениво при первом запросе
        private void EnsureInitialized()
        {
            if (initialized) return;

            if (registry == null)
            {
                ServiceDebug.LogError(this, "registry не назначен");
                return;
            }

            initialized = true;

            ValidateLinks();
            SeedEverCompleted();
            SeedUnlockedFromEverCompleted();
            SubscribeConditionSteps();
            EvaluateUnlocks(false);
            SeedVisibleBlocks();
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
            initialized = false;
        }

        #endregion

        #region Queries

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

        #endregion

        #region Progress

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
        }

        #endregion

        #region UI list API

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

        #region Map API

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

        /// <summary> Транзитивно открыть все достижимые диорамы; при notify — события на новые </summary>
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
                    // монотонно: «когда-либо пройдена», чтобы откат шага источника не закрывал цель
                    return link.Source != null && everCompleted.Contains(link.Source.Id);

                case DioramaLinkCondition.StepTrigger:
                    return IsStepTriggerSatisfied(link);

                default:
                    ServiceDebug.LogError(this, $"Необработанное условие связи: {link.Condition}");
                    return false;
            }
        }

        private static bool IsStepTriggerSatisfied(DioramaLink link)
        {
            var step = link.Step;
            if (step == null) return false;

            var trigger = link.StepTrigger;
            // reject- и change-триггеры не задают стационарного состояния — открыть по ним нельзя
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
                        ServiceDebug.LogWarning(this, $"Связь диорамы {def.name}: источник не назначен");

                    if (link.Condition != DioramaLinkCondition.StepTrigger) continue;

                    if (link.Step == null)
                        ServiceDebug.LogWarning(this, $"Связь диорамы {def.name}: шаг-условие не назначен");
                    else if (link.StepTrigger.IsRejection() || link.StepTrigger.IsChange())
                        ServiceDebug.LogError(this,
                            $"Связь диорамы {def.name}: триггер {link.StepTrigger} не задаёт состояние — связь не откроется");
                }
            }
        }

        #endregion
    }
}
