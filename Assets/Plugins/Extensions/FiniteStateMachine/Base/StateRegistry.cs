using System;
using System.Collections.Generic;
using Extensions.FiniteStateMachine;

namespace Blackset.Duel.Sequence
{
    /// <summary>
    /// Реестр всех доступных состояний машины
    /// </summary>
    public class StateRegistry<TContext> : IStateRegistry<TContext>
    {
        protected readonly Dictionary<Type, IState<TContext>> states = new();

        public virtual void Add<TState>(TState state) where TState : class, IState<TContext>
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            states[typeof(TState)] = state;
        }
        
        public virtual IState<TContext> Get(Type stateType)
        {
            if (states.TryGetValue(stateType, out IState<TContext> state))
                return state;

            if (!typeof(IState<TContext>).IsAssignableFrom(stateType))
                throw new InvalidOperationException(
                    $"Тип '{stateType.Name}' не реализует IState<{typeof(TContext).Name}>.");

            if (Activator.CreateInstance(stateType) is not IState<TContext> createdState)
                throw new InvalidOperationException(
                    $"Не удалось создать состояние '{stateType.Name}'. Проверьте наличие публичного конструктора без параметров.");

            states.Add(stateType, createdState);
            return createdState;
        }
    }
}