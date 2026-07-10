using System.Collections.Generic;
using DioramaEnigma.Dioramas;
using DioramaEnigma.Sequences;

namespace DioramaEnigma.Quests
{
    /// <summary>
    /// Выбирает ближайшие по очереди квесты блока для панели заданий
    /// </summary>
    /// <remarks>
    /// Отделён от UI, чтобы будущая система подсказок переиспользовала ту же выборку.
    /// Приоритет не поднимает квест наверх — все идут строго в порядке очереди; он лишь
    /// гарантирует попадание ближайших приоритетных. Опционально держит выполненные квесты
    /// (метка «готово») до завершения приоритетного, стоящего за ними
    /// </remarks>
    public static class QuestSelector
    {
        private readonly struct Candidate
        {
            public readonly string StepId;
            public readonly string Text;
            public readonly bool Priority;
            public readonly bool Completed;
            public readonly bool ConditionsMet;
            public readonly DioramaDefinition Diorama;

            public Candidate(string stepId, string text, bool priority, bool completed, bool conditionsMet, DioramaDefinition diorama)
            {
                StepId = stepId;
                Text = text;
                Priority = priority;
                Completed = completed;
                ConditionsMet = conditionsMet;
                Diorama = diorama;
            }
        }

        /// <summary>
        /// Ближайшие квесты блока в порядке очереди
        /// </summary>
        /// <param name="blockOrder"> Порядок диорам блока (для сортировки живых инстансов) </param>
        /// <param name="live"> Живые инстансы блока </param>
        /// <param name="focused"> Сфокусированная диорама (для фильтра focusedOnly) </param>
        /// <param name="options"> Параметры выборки </param>
        public static List<ActiveQuest> Select(
            IReadOnlyList<DioramaDefinition> blockOrder,
            IReadOnlyCollection<DioramaInstance> live,
            DioramaDefinition focused,
            QuestPanelOptions options)
        {
            var result = new List<ActiveQuest>();
            if (live == null || live.Count == 0) return result;

            var ordered = CollectOrderedInstances(blockOrder, live, focused, options.FocusedOnly);
            var candidates = CollectCandidates(ordered, options);
            bool[] heldAllowed = ComputeHeldAllowed(candidates);

            int normalTaken = 0;
            int priorityTaken = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];

                if (candidate.Completed)
                {
                    if (options.HoldBeforePriority && !candidate.Priority && heldAllowed[i])
                        result.Add(ToQuest(candidate, done: true));

                    continue;
                }

                if (!candidate.ConditionsMet) continue;

                if (candidate.Priority)
                {
                    if (priorityTaken >= options.PriorityCount) continue;
                    priorityTaken++;
                }
                else
                {
                    if (normalTaken >= options.NormalCount) continue;
                    normalTaken++;
                }

                result.Add(ToQuest(candidate, done: false));
            }

            return result;
        }

        private static List<DioramaInstance> CollectOrderedInstances(
            IReadOnlyList<DioramaDefinition> blockOrder,
            IReadOnlyCollection<DioramaInstance> live,
            DioramaDefinition focused,
            bool focusedOnly)
        {
            var ordered = new List<DioramaInstance>();

            foreach (var instance in live)
            {
                if (instance == null || instance.Definition == null) continue;
                if (focusedOnly && instance.Definition != focused) continue;

                ordered.Add(instance);
            }

            ordered.Sort((a, b) => BlockIndex(blockOrder, a).CompareTo(BlockIndex(blockOrder, b)));
            return ordered;
        }

        private static int BlockIndex(IReadOnlyList<DioramaDefinition> blockOrder, DioramaInstance instance)
        {
            if (blockOrder == null) return int.MaxValue;

            for (int i = 0; i < blockOrder.Count; i++)
                if (blockOrder[i] == instance.Definition) return i;

            return int.MaxValue;
        }

        private static List<Candidate> CollectCandidates(List<DioramaInstance> ordered, QuestPanelOptions options)
        {
            var candidates = new List<Candidate>();

            foreach (var instance in ordered)
            {
                var sequence = instance.Definition.Sequence;
                if (sequence == null) continue;

                AppendCandidates(candidates, sequence, instance.Definition, options);
            }

            return candidates;
        }

        private static void AppendCandidates(
            List<Candidate> candidates, Sequence sequence, DioramaDefinition diorama, QuestPanelOptions options)
        {
            int startCount = candidates.Count;

            foreach (var entry in sequence.Steps)
            {
                var step = entry?.Step;
                if (step == null || !step.IsQuestStep) continue;
                if (string.IsNullOrEmpty(step.QuestText)) continue;
                if (!PassesGroupFilter(entry.Availability, options)) continue;

                candidates.Add(new Candidate(
                    step.Id, step.QuestText, step.IsPriorityQuest,
                    step.IsCompleted, ConditionsMet(step), diorama));
            }

            // Внутри диорамы упорядочиваем по группе (массив Steps порядок групп не гарантирует)
            candidates.Sort(startCount, candidates.Count - startCount,
                Comparer<Candidate>.Create((a, b) => GroupOf(sequence, a).CompareTo(GroupOf(sequence, b))));
        }

        private static bool PassesGroupFilter(GroupAvailability availability, QuestPanelOptions options)
        {
            bool always = availability == GroupAvailability.Always;
            return always ? options.ShowAlways : options.ShowLinear;
        }

        private static bool ConditionsMet(AbstractSequenceStep step)
        {
            if (!step.IgnoreUnlocked && !step.IsUnlocked) return false;

            var gates = step.QuestGates;
            if (gates == null) return true;

            foreach (var gate in gates)
                if (gate != null && !gate.IsSatisfied())
                    return false;

            return true;
        }

        private static bool[] ComputeHeldAllowed(List<Candidate> candidates)
        {
            var heldAllowed = new bool[candidates.Count];
            bool nearestPriorityAfterIsPending = false;

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                heldAllowed[i] = nearestPriorityAfterIsPending;

                if (candidates[i].Priority)
                    nearestPriorityAfterIsPending = !candidates[i].Completed;
            }

            return heldAllowed;
        }

        private static int GroupOf(Sequence sequence, Candidate candidate)
        {
            foreach (var entry in sequence.Steps)
                if (entry?.Step != null && entry.Step.Id == candidate.StepId)
                    return entry.GroupIndex;

            return int.MaxValue;
        }

        private static ActiveQuest ToQuest(Candidate candidate, bool done) =>
            new(candidate.StepId, candidate.Text, candidate.Priority, candidate.Diorama, done);
    }
}
