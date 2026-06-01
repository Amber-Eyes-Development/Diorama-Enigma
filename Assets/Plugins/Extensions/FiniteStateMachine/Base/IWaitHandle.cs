namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Единая точка манипуляцией ожиданием
    /// </summary>
    public interface IWaitHandle
    {
        /// <summary>
        /// Состояния ожидания
        /// </summary>
        bool IsDone { get; }
        /// <summary>
        /// Отмена ожидания
        /// </summary>
        void Cancel();
    }
}