namespace Extensions.Helpers.LifecycleActions
{
    /// <summary>
    /// Точки жизненного цикла <see cref="MonoBehaviour"/>
    /// </summary>
    public enum LifecycleMoment
    {
        Awake = 0,
        OnEnable = 1,
        Start = 2,
        OnDisable = 3,
        OnDestroy = 4
    }
}