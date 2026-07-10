namespace DioramaEnigma.Quests
{
    /// <summary>
    /// Параметры выборки квестов для панели заданий
    /// </summary>
    public readonly struct QuestPanelOptions
    {
        /// <summary> Сколько обычных квестов показывать </summary>
        public int NormalCount { get; }
        /// <summary> Сколько приоритетных квестов гарантировать </summary>
        public int PriorityCount { get; }
        /// <summary> Только квесты сфокусированной диорамы </summary>
        public bool FocusedOnly { get; }
        /// <summary> Удерживать выполненные квесты до приоритетного впереди (метка «готово» вместо исчезновения) </summary>
        public bool HoldBeforePriority { get; }
        /// <summary> Показывать квесты линейных шагов </summary>
        public bool ShowLinear { get; }
        /// <summary> Показывать квесты Always-шагов (фоновых) </summary>
        public bool ShowAlways { get; }

        public QuestPanelOptions(int normalCount, int priorityCount, bool focusedOnly,
            bool holdBeforePriority, bool showLinear, bool showAlways)
        {
            NormalCount = normalCount;
            PriorityCount = priorityCount;
            FocusedOnly = focusedOnly;
            HoldBeforePriority = holdBeforePriority;
            ShowLinear = showLinear;
            ShowAlways = showAlways;
        }
    }
}
