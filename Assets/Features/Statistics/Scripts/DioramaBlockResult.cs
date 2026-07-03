using System.Collections.Generic;
using DioramaEnigma.Dioramas;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Итог прохождения блока: тайминги по диорамам, суммарное время и рекорд
    /// </summary>
    public sealed class DioramaBlockResult
    {
        /// <summary> Пройденный блок </summary>
        public DioramaBlock Block { get; }
        /// <summary> Тайминги по диорамам блока (в порядке блока) </summary>
        public IReadOnlyList<Entry> Dioramas { get; }
        /// <summary> Суммарное время блока, секунд </summary>
        public float TotalSeconds { get; }
        /// <summary> Количество диорам в блоке </summary>
        public int DioramaCount { get; }
        /// <summary> Прежний рекорд блока, секунд (0, если рекорда ещё не было) </summary>
        public float BestSeconds { get; }
        /// <summary> Текущий прогон стал новым рекордом </summary>
        public bool IsNewRecord { get; }

        public DioramaBlockResult(DioramaBlock block, IReadOnlyList<Entry> dioramas,
            float totalSeconds, float bestSeconds, bool isNewRecord)
        {
            Block = block;
            Dioramas = dioramas;
            TotalSeconds = totalSeconds;
            DioramaCount = dioramas?.Count ?? 0;
            BestSeconds = bestSeconds;
            IsNewRecord = isNewRecord;
        }

        /// <summary>
        /// Тайминг одной диорамы
        /// </summary>
        public readonly struct Entry
        {
            /// <summary> Диорама </summary>
            public DioramaDefinition Diorama { get; }
            /// <summary> Время на диораму, секунд </summary>
            public float Seconds { get; }

            public Entry(DioramaDefinition diorama, float seconds)
            {
                Diorama = diorama;
                Seconds = seconds;
            }
        }
    }
}
