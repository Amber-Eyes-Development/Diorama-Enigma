namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Действие анимации при прямом срабатывании триггера
    /// </summary>
    public enum AnimationCommand
    {
        /// <summary> Проиграть вперёд </summary>
        Play = 0,
        /// <summary> Проиграть назад (твин в обратную сторону) </summary>
        PlayBackwards = 1,
        /// <summary> Остановить (в начало) </summary>
        Stop = 2,
    }
}
