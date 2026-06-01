using System;

namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Реестр всех доступных состояний машины
    /// </summary>
    /// <typeparam name="TContext">Входные данные стейт-машины</typeparam>
    public interface IStateRegistry<TContext>
    {
        /// <summary>
        /// Добавить состояние в реестр
        /// </summary>
        /// <typeparam name="TState">Тип состояния</typeparam>
        /// <param name="state">Экземпляр состояния</param>
        void Add<TState>(TState state) where TState : class, IState<TContext>;

        /// <summary>
        /// Получить состояние по типу
        /// </summary>
        /// <param name="stateType">Тип состояния</param>
        /// <returns>Состояние</returns>
        IState<TContext> Get(Type stateType);
    }
}