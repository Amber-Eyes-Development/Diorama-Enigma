using System.Collections.Generic;

namespace Extensions.Sequences
{
    /// <summary>
    /// Контекст сборки графа из <see cref="StepDefinition"/>: кэширует «один definition → один POCO»
    /// (общий шаг, на который ссылаются несколько мест, строится один раз) и ведёт карту id → шаг.
    /// </summary>
    public sealed class BuildContext
    {
        private readonly Dictionary<StepDefinition, Step> built = new();
        private readonly Dictionary<string, Step> byId = new();

        /// <summary> Построить шаг из definition (или вернуть ранее построенный) </summary>
        public Step GetOrBuild(StepDefinition definition)
        {
            if (definition == null) return null;
            if (built.TryGetValue(definition, out var existing)) return existing;

            var step = definition.Build(this);
            built[definition] = step;

            if (step != null && !string.IsNullOrEmpty(step.Id))
                byId[step.Id] = step;

            return step;
        }

        /// <summary> Найти уже построенный шаг по id </summary>
        public Step Resolve(string id) =>
            !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out var step) ? step : null;
    }
}
