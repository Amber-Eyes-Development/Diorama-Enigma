namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Состояние машины
    /// </summary>
    /// <typeparam name="TContext">Входные данные стейт-машины</typeparam>
    public interface IState<TContext>
    {
        /// <summary>
        /// Запуск состояния
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        public void Enter(TContext context);
        /// <summary>
        /// Тик состояния
        /// </summary>
        /// <returns>Результат исполнения тика состояния</returns>
        /// <param name="context">Входные данные стейт-машины</param>
        public StateResult Tick(TContext context);
        /// <summary>
        /// Выход из состояния
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        public void Exit(TContext context);
    }
}