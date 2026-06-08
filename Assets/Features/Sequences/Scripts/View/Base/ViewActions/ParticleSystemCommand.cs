namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Команда для системы частиц
    /// </summary>
    public enum ParticleSystemCommand
    {
        /// <summary> Запустить эмиссию </summary>
        Play,
        /// <summary> Остановить эмиссию (живые частицы доигрывают, новые не спавнятся) </summary>
        Stop,
    }
}
