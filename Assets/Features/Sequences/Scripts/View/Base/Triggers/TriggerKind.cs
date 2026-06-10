namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Тип события шага, на которое реагируют вьюшки/эффекты/гейты
    /// </summary>
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
        /// <summary> Попытка взаимодействия с недоступным шагом (залочен или уже завершён) — импульс без стационарного состояния </summary>
        InteractionRejected = 6,
    }
}
