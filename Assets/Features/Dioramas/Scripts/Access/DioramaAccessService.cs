using System;
using System.Collections.Generic;
using DioramaEnigma.Sequences;
using Extensions.Log;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Сервис доступа: вычисляет открытые/пройденные диорамы по графу входящих связей
    /// </summary>
    /// <remarks>
    /// Откат шагов учтён разделением двух понятий:
    /// «пройдена сейчас» (<see cref="IsCompleted"/>) — живое <see cref="DioramaDefinition.IsCompleted"/>,
    /// отражает откат шагов; «пройдена когда-либо» (everCompleted) — монотонно, персистится снимком и
    /// держит открытость стабильной (откат шага не закрывает уже открытые диорамы). Поэтому снимок
    /// не «залипает» в UI-состоянии: серость диорамы определяется живой завершённостью
    /// </remarks>
    public sealed class DioramaAccessService
    {
        private const char SNAPSHOT_SEPARATOR = ';';

        /// <summary> Диорама открыта </summary>
        public event Action<DioramaDefinition> onDioramaUnlocked;
        /// <summary> Диорама пройдена впервые </summary>
        public event Action<DioramaDefinition> onDioramaCompleted;
        /// <summary> Блок стал виден (появилась первая открытая диорама) </summary>
        public event Action<DioramaBlock> onBlockUnlocked;

        private readonly DioramaRegistry registry;
        private readonly DioramaCompletedSnapshot completedSnapshot;

        private readonly HashSet<string> unlocked = new();
        private readonly HashSet<string> everCompleted = new();
        private readonly HashSet<string> visibleBlocks = new();
        private readonly List<SequenceStep> subscribedSteps = new();

        private bool initialized;

        public DioramaAccessService(DioramaRegistry registry, DioramaCompletedSnapshot completedSnapshot)
        {
            this.registry = registry;
            this.completedSnapshot = completedSnapshot;
        }

        #region Lifecycle

        /// <summary> Построить граф доступа из персистентного состояния и подписаться на шаги-условия </summary>
        public void Initialize()
        {
            if (initialized) return;

            if (registry == null)
            {
                ServiceDebug.LogError<DioramaAccessService>("registry не назначен");
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

        /// <summary> Снять подписки на шаги-условия </summary>
        public void Dispose()
        {
            foreach (var step in subscribedSteps)
            {
                if (step == null) continue;

                step.onCompletionChanged -= OnConditionStepChanged;
                step.onUnlockChanged -= OnConditionStepChanged;
            }

            subscribedSteps.Clear();
            initialized = false;
        }

        #endregion

        #region Queries

        /// <summary> Открыта ли диорама </summary>
        public bool IsUnlocked(DioramaDefinition def) => def != null && unlocked.Contains(def.Id);

        /// <summary> Пройдена ли диорама прямо сейчас (с учётом отката шагов) </summary>
        public bool IsCompleted(DioramaDefinition def) => def != null && def.IsCompleted;

        /// <summary> Состояние доступа диорамы </summary>
        public DioramaState StateOf(DioramaDefinition def)
        {
            if (def == null) return DioramaState.Locked;
            if (def.IsCompleted) return DioramaState.Completed;
            if (unlocked.Contains(def.Id)) return DioramaState.Unlocked;

            return DioramaState.Locked;
        }

        /// <summary> Определение по идентификатору (или null) </summary>
        /// <param name="id">Идентификатор диорамы</param>
        public DioramaDefinition ById(string id) => registry != null ? registry.ById(id) : null;

        #endregion

        #region Progress

        /// <summary>
        /// Отметить диораму пройденной (вызывается при завершении её раннера) и каскадно переоценить доступ
        /// </summary>
        /// <param name="def">Пройденная диорама</param>
        public void MarkCompleted(DioramaDefinition def)
        {
            if (!initialized || def == null) return;
            if (!everCompleted.Add(def.Id)) return;

            unlocked.Add(def.Id);
            AppendSnapshot(def.Id);
            onDioramaCompleted?.Invoke(def);
            EvaluateUnlocks(true);
        }

        #endregion

        #region UI list API (#4)

        /// <summary> Блоки с хотя бы одной открытой диорамой, в порядке реестра </summary>
        public IReadOnlyList<DioramaBlock> VisibleBlocks()
        {
            var result = new List<DioramaBlock>();

            foreach (var entry in registry.Blocks)
            {
                if (entry?.Block == null) continue;
                if (HasUnlockedDiorama(entry)) result.Add(entry.Block);
            }

            return result;
        }

        /// <summary> Открытые диорамы блока с их состоянием (закрытые скрыты), в порядке блока </summary>
        /// <param name="block">Блок</param>
        public IReadOnlyList<DioramaNode> DioramasInBlock(DioramaBlock block)
        {
            var result = new List<DioramaNode>();

            foreach (var def in registry.InBlock(block))
            {
                if (def == null || !unlocked.Contains(def.Id)) continue;
                result.Add(new DioramaNode(def, StateOf(def)));
            }

            return result;
        }

        /// <summary> Все открытые диорамы в порядке реестра (очередь для спавна/навигации) </summary>
        public IReadOnlyList<DioramaDefinition> VisibleOrdered()
        {
            var result = new List<DioramaDefinition>();

            foreach (var def in registry.Ordered())
                if (def != null && unlocked.Contains(def.Id))
                    result.Add(def);

            return result;
        }

        #endregion

        #region Map API (#5)

        /// <summary> Построить срез карты: все диорамы как узлы (с состоянием) и связи как рёбра </summary>
        public DioramaMapView BuildMap()
        {
            var nodes = new List<DioramaNode>();
            var edges = new List<DioramaEdge>();

            foreach (var def in registry.Ordered())
            {
                if (def == null) continue;

                nodes.Add(new DioramaNode(def, StateOf(def)));

                foreach (var link in def.IncomingLinks)
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
            foreach (var def in registry.Ordered())
                if (def != null && def.IsCompleted)
                    everCompleted.Add(def.Id);

            foreach (var id in ParseSnapshot())
                everCompleted.Add(id);
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

            foreach (var def in registry.Ordered())
            {
                if (def == null) continue;

                foreach (var link in def.IncomingLinks)
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

                foreach (var def in registry.Ordered())
                {
                    if (def == null || unlocked.Contains(def.Id)) continue;
                    if (!ShouldUnlock(def)) continue;

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

        private bool ShouldUnlock(DioramaDefinition def)
        {
            if (def.UnlockedFromStart) return true;

            foreach (var link in def.IncomingLinks)
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
                    ServiceDebug.LogError<DioramaAccessService>($"Необработанное условие связи: {link.Condition}");
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
            foreach (var def in registry.Ordered())
            {
                if (def == null || !unlocked.Contains(def.Id)) continue;

                var block = registry.BlockOf(def);
                if (block != null) visibleBlocks.Add(block.Id);
            }
        }

        #endregion

        #region Helpers

        private bool HasUnlockedDiorama(DioramaBlockEntry entry)
        {
            foreach (var def in entry.Dioramas)
                if (def != null && unlocked.Contains(def.Id)) return true;

            return false;
        }

        private IEnumerable<string> ParseSnapshot()
        {
            string csv = completedSnapshot != null ? completedSnapshot.Value : null;
            if (string.IsNullOrEmpty(csv)) yield break;

            foreach (var id in csv.Split(SNAPSHOT_SEPARATOR))
                if (!string.IsNullOrEmpty(id)) yield return id;
        }

        private void AppendSnapshot(string id)
        {
            if (completedSnapshot == null) return;

            string csv = completedSnapshot.Value;
            completedSnapshot.SetValue(string.IsNullOrEmpty(csv) ? id : $"{csv}{SNAPSHOT_SEPARATOR}{id}");
        }

        /// <summary> Предупредить о некорректной конфигурации связей (как ValidateTriggers у раннера) </summary>
        private void ValidateLinks()
        {
            foreach (var def in registry.Ordered())
            {
                if (def == null) continue;

                foreach (var link in def.IncomingLinks)
                {
                    if (link == null) continue;

                    if (link.Source == null)
                        ServiceDebug.LogWarning<DioramaAccessService>($"Связь диорамы {def.name}: источник не назначен");

                    if (link.Condition != DioramaLinkCondition.StepTrigger) continue;

                    if (link.Step == null)
                        ServiceDebug.LogWarning<DioramaAccessService>($"Связь диорамы {def.name}: шаг-условие не назначен");
                    else if (link.StepTrigger.IsRejection() || link.StepTrigger.IsChange())
                        ServiceDebug.LogError<DioramaAccessService>(
                            $"Связь диорамы {def.name}: триггер {link.StepTrigger} не задаёт состояние — связь не откроется");
                }
            }
        }

        #endregion
    }
}
