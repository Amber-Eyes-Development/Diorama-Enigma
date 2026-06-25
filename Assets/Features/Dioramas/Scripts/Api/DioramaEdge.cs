namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Ребро карты: связь от диорамы-источника к цели и условие открытия
    /// </summary>
    public sealed class DioramaEdge
    {
        /// <summary> Диорама-источник связи </summary>
        public DioramaDefinition From { get; }
        /// <summary> Диорама-цель связи </summary>
        public DioramaDefinition To { get; }
        /// <summary> Условие открытия по этой связи </summary>
        public DioramaLinkCondition Condition { get; }

        public DioramaEdge(DioramaDefinition from, DioramaDefinition to, DioramaLinkCondition condition)
        {
            From = from;
            To = to;
            Condition = condition;
        }
    }
}
