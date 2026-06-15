namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Тип события шага, на которое реагируют вьюшки/эффекты/гейты
    /// </summary>
    /// <remarks>
    /// Значения &lt; 10 — события состояния шага (завершённость/разблокировка): их понимают раннер, гейты и эффекты;
    /// Значения &gt;= 10 — импульсы-отказы взаимодействия (фидбэк только для вьюшек)
    /// </remarks>
    public enum TriggerKind
    {
        /// <summary> Шаг завершён </summary>
        Completed = 0,
        /// <summary> Шаг не завершён </summary>
        NotCompleted = 1,
        /// <summary> Шаг стал доступен для изменения </summary>
        Unlocked = 2,
        /// <summary> Шаг стал недоступен для изменения </summary>
        Locked = 3,
        /// <summary> Завершённость изменилась на любое значение </summary>
        CompletionStateChange = 4,
        /// <summary> Разблокированность изменилась на любое значение </summary>
        LockStateChange = 5,

        /// <summary> Попытка взаимодействия с недоступным шагом
        /// (неактивен в раннере или закрыт гейтом) </summary>
        InteractionRejected = 10,
        /// <summary> Попытка взаимодействия с недоступным шагом
        /// (закрыт гейтом, но активен в раннере) </summary>
        UnlockedInteractionRejected = 11,
        /// <summary> Попытка взаимодействия с недоступным шагом
        /// (неактивен в раннере) </summary>
        LockedInteractionRejected = 12,
    }
}
