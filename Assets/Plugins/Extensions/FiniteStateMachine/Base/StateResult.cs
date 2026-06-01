using System;

namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Результат исполнения тика состояния
    /// </summary>
    public readonly struct StateResult
    {
        /// <summary>
        /// Тип перехода
        /// </summary>
        public StateTransition Transition { get; }
        /// <summary>
        /// Тип следующего состояния
        /// </summary>
        public Type NextStateType { get; }

        private StateResult(StateTransition transition, Type nextStateType = null)
        {
            Transition = transition;
            NextStateType = nextStateType;
        }

        /// <summary>
        /// Остаться в текущем состоянии
        /// </summary>
        public static StateResult Stay() =>
            new StateResult(StateTransition.Stay);

        /// <summary>
        /// Запросить завершение работы FSM
        /// </summary>
        public static StateResult Stop() =>
            new StateResult(StateTransition.Stop);

        /// <summary>
        /// Переключить состояние (сбрасывает стек)
        /// </summary>
        public static StateResult Switch<TState>() where TState : class =>
            new StateResult(StateTransition.Switch, typeof(TState));

        /// <summary>
        /// Положить текущее состояние в стек и перейти в новое
        /// </summary>
        public static StateResult Push<TState>() where TState : class =>
            new StateResult(StateTransition.Push, typeof(TState));

        /// <summary>
        /// Вернуться к предыдущему состоянию из стека
        /// </summary>
        public static StateResult Pop() =>
            new StateResult(StateTransition.Pop);
    }
}