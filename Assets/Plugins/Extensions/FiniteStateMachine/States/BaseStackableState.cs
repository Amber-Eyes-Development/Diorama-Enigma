namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Абстракция базового состояния с возможность паузы без прерывания
    /// </summary>
    /// <typeparam name="TContext">Входные данные стейт-машины</typeparam>
    public abstract class BaseStackableState<TContext> : BaseState<TContext>, IStackableState<TContext>
    {
        /// <summary>
        /// Поставить текущий стейт на паузу
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        public virtual void Pause(TContext context) { }
        /// <summary>
        /// Продолжить выполнение текущего стейта
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        public virtual void Resume(TContext context) { }
        /// <summary>
        /// Форсированное завершение стейта
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        public virtual void ForceExit(TContext context) => Exit(context);
    }
}