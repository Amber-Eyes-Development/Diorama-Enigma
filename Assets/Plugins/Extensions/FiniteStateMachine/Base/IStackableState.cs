namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Возможность паузы без прерывания для стейта
    /// </summary>
    /// <typeparam name="TContext">Входные данные стейт-машины</typeparam>
    public interface IStackableState<TContext>
    {
        /// <summary>
        /// Поставить текущий стейт на паузу
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        void Pause(TContext context);
        /// <summary>
        /// Продолжить выполнение текущего стейта
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        void Resume(TContext context);
        /// <summary>
        /// Форсированное завершение стейта
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        void ForceExit(TContext context); 
    }
}