namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Узел карты/списка: диорама и её текущее состояние доступа
    /// </summary>
    public sealed class DioramaNode
    {
        /// <summary> Определение диорамы </summary>
        public DioramaDefinition Definition { get; }
        /// <summary> Состояние доступа на момент построения </summary>
        public DioramaState State { get; }

        public DioramaNode(DioramaDefinition definition, DioramaState state)
        {
            Definition = definition;
            State = state;
        }
    }
}
