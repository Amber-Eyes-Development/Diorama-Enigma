namespace Extensions.SceneFlow
{
    /// <summary>
    /// Шаг загрузки целевой сцены
    /// </summary>
    /// <remarks>
    /// Игровая система реализует интерфейс, регистрируется в <see cref="SceneLoadCoordinator"/>
    /// и сообщает свой прогресс/готовность. Координатор не отпускает экран загрузки,
    /// пока все зарегистрированные шаги не завершатся. Контракт опционален
    /// </remarks>
    public interface ISceneLoadStep
    {
        /// <summary>
        /// Прогресс шага от 0 до 1
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// Шаг завершён
        /// </summary>
        bool IsDone { get; }
    }
}
