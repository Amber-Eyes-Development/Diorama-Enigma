namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Единая точка манипуляцией ожиданием (пустая)
    /// </summary>
    public sealed class EmptyWaitHandle : IWaitHandle // TODO необходим ли?
    {
        /// <summary>
        /// Синглтон хэндла
        /// </summary>
        public static readonly IWaitHandle Instance = new EmptyWaitHandle();

        /// <summary>
        /// Состояния ожидания
        /// </summary>
        public bool IsDone => true;

        private EmptyWaitHandle() { }
        
        /// <summary>
        /// Отмена ожидания
        /// </summary>
        public void Cancel() { }
    }
}