namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Узел карты/списка: диорама и её текущее состояние доступа
    /// </summary>
    public sealed class DioramaNode
    {
        /// <summary> Определение диорамы </summary>
        public DioramaDefinition Definition { get; }
        /// <summary> Состояние доступа на момент построения (Completed = решена, трек №2) </summary>
        public DioramaState State { get; }
        /// <summary> Полностью ли пройдены все шаги, вкл. фоновые (трек №1) — для UI-индикации «ещё есть что поделать» </summary>
        public bool FullyCompleted { get; }

        public DioramaNode(DioramaDefinition definition, DioramaState state, bool fullyCompleted)
        {
            Definition = definition;
            State = state;
            FullyCompleted = fullyCompleted;
        }
    }
}
