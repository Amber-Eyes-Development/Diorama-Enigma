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
    /// гарантирует попадание ближайших приоритетных, даже если они дальше N-го обычного
    /// </remarks>
    public static class QuestSelector
    {
        /// <summary>
        /// Ближайшие квесты блока в порядке очереди
        /// </summary>
        /// <param name="blockOrder"> Порядок диорам блока (для сортировки живых инстансов) </param>
        /// <param name="live"> Живые инстансы блока </param>
        /// <param name="focused"> Сфокусированная диорама (для фильтра focusedOnly) </param>
        /// <param name="focusedOnly"> Только квесты сфокусированной диорамы </param>
        /// <param name="normalCount"> Сколько обычных квестов показывать </param>
        /// <param name="priorityCount"> Сколько приоритетных квестов гарантировать </param>
        public static List<ActiveQuest> Select(
            IReadOnlyList<DioramaDefinition> blockOrder,
            IReadOnlyCollection<DioramaInstance> live,
            DioramaDefinition focused,
            bool focusedOnly,
            int normalCount,
            int priorityCount)
        {
            var result = new List<ActiveQuest>();
            if (live == null || live.Count == 0) return result;

            var ordered = CollectOrderedInstances(blockOrder, live, focused, focusedOnly);
            var pending = CollectPendingQuests(ordered);

            int normalTaken = 0;
            int priorityTaken = 0;

            foreach (var quest in pending)
            {
                if (quest.Priority)
                {
                    if (priorityTaken >= priorityCount) continue;
                    priorityTaken++;
                }
                else
                {
                    if (normalTaken >= normalCount) continue;
                    normalTaken++;
                }

                result.Add(quest);
            }

            return result;
        }

        // Живые инстансы в порядке блока (либо только сфокусированный)
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

        // Плоский список ожидающих квестовых шагов: диорамы по очереди, шаги внутри — по GroupIndex
        private static List<ActiveQuest> CollectPendingQuests(List<DioramaInstance> ordered)
        {
            var pending = new List<ActiveQuest>();

            foreach (var instance in ordered)
            {
                var sequence = instance.Definition.Sequence;
                if (sequence == null) continue;

                AppendPendingQuests(pending, sequence, instance.Definition);
            }

            return pending;
        }

        private static void AppendPendingQuests(List<ActiveQuest> pending, Sequence sequence, DioramaDefinition diorama)
        {
            int startCount = pending.Count;

            foreach (var entry in sequence.Steps)
            {
                var step = entry?.Step;
                if (step == null || !step.IsQuestStep || step.IsCompleted) continue;
                if (string.IsNullOrEmpty(step.QuestText)) continue;

                pending.Add(new ActiveQuest(step.Id, step.QuestText, step.IsPriorityQuest, diorama));
            }

            // Внутри диорамы упорядочиваем по группе (массив Steps порядок групп не гарантирует)
            pending.Sort(startCount, pending.Count - startCount,
                Comparer<ActiveQuest>.Create((a, b) => GroupOf(sequence, a).CompareTo(GroupOf(sequence, b))));
        }

        private static int GroupOf(Sequence sequence, ActiveQuest quest)
        {
            foreach (var entry in sequence.Steps)
                if (entry?.Step != null && entry.Step.Id == quest.StepId)
                    return entry.GroupIndex;

            return int.MaxValue;
        }
    }
}
