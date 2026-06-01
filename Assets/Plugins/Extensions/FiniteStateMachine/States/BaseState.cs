namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Абстракция базового состояния
    /// </summary>
    /// <typeparam name="TContext">Входные данные стейт-машины</typeparam>
    public abstract class BaseState<TContext> : IState<TContext>
    {
        /// <summary>
        /// Запуск состояния
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        public virtual void Enter(TContext context) { }
        /// <summary>
        /// Тик состояния
        /// </summary>
        /// <returns>Результат исполнения тика состояния</returns>
        /// <param name="context">Входные данные стейт-машины</param>
        public abstract StateResult Tick(TContext context);
        /// <summary>
        /// Выход из состояния
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        public virtual void Exit(TContext context) { }
    }
}