namespace Extensions.Sequences
{
    /// <summary>
    /// Тип события шага, на которое реагируют гейты/эффекты/композиты/вьюшки
    /// </summary>
    /// <remarks>
    /// Значения &lt; 10 — события состояния шага (завершённость/разблокировка): их понимают движок, гейты и эффекты;
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

        /// <summary> Попытка взаимодействия с недоступным шагом (неактивен в движке или закрыт гейтом) </summary>
        InteractionRejected = 10,
        /// <summary> Попытка взаимодействия с недоступным шагом (закрыт гейтом, но активен) </summary>
        UnlockedInteractionRejected = 11,
        /// <summary> Попытка взаимодействия с недоступным шагом (неактивен) </summary>
        LockedInteractionRejected = 12,
    }
}
