using System;

namespace Extensions.Sequences
{
    /// <summary>
    /// Сериализуемый снимок последовательности: граф (структура) + состояние.
    /// «Плоские» DTO с явным дискриминатором <see cref="StepData.Kind"/> → round-trip через любой бэкенд без TypeNameHandling.
    /// Ссылки внутри графа — по id шагов.
    /// </summary>
    [Serializable]
    public sealed class SequenceSnapshot
    {
        /// <summary> Id последовательности </summary>
        public string Id;
        /// <summary> Каталог всех шагов (включая дочерние шаги композитов) </summary>
        public StepData[] Steps = Array.Empty<StepData>();
        /// <summary> Записи последовательности (ссылаются на шаги каталога по id) </summary>
        public EntryData[] Entries = Array.Empty<EntryData>();

        /// <summary> Тип шага в снапшоте </summary>
        public enum StepKind { Bool = 0, Counter = 1, Composite = 2, Custom = 99 }
        /// <summary> Тип гейта в снапшоте </summary>
        public enum GateKind { StepState = 0, Custom = 99 }
        /// <summary> Тип эффекта в снапшоте (поведенческие, напр. CallbackEffect, не сериализуются) </summary>
        public enum EffectKind { SetStep = 0, Custom = 99 }

        /// <summary> Данные шага (поля-«объединение» по типу) </summary>
        [Serializable]
        public struct StepData
        {
            /// <summary> Id шага </summary>
            public string Id;
            /// <summary> Тип (<see cref="StepKind"/>) </summary>
            public int Kind;
            /// <summary> Необратимость </summary>
            public bool Irreversible;

            // Bool
            public bool BoolValue;
            public bool CompletionState;

            // Counter
            public int Count;
            public int Target;

            // Composite
            public int Mode;
            public int N;
            public string[] ChildIds;
            public int[] ChildTriggers;

            /// <summary> Escape-hatch для кастомных типов шагов </summary>
            public string PayloadJson;
        }

        /// <summary> Данные записи последовательности </summary>
        [Serializable]
        public struct EntryData
        {
            public string StepId;
            public int Group;
            public int Availability;
            public bool InteractableAfterCompletion;
            public GateData[] Gates;
            public EffectData[] Effects;
        }

        /// <summary> Данные гейта </summary>
        [Serializable]
        public struct GateData
        {
            public int Kind;
            public int Trigger;
            public string[] StepIds;
            public string PayloadJson;
        }

        /// <summary> Данные эффекта записи (триггер + эффект) </summary>
        [Serializable]
        public struct EffectData
        {
            public int Trigger;
            public int Kind;
            public string TargetId;
            public bool Value;
            public string PayloadJson;
        }
    }
}
