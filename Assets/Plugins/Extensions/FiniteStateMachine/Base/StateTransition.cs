namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Тип перехода в состояние
    /// </summary>
    public enum StateTransition
    {
        Stay = 0, // Остаться в текущем состоянии
        Switch = 1, // Переключить состояние на другое
        Push = 2, // Push состояния
        Pop = 3, // Pop состояния
        Stop = 4 // Запрос завершения работы машины
    }
}