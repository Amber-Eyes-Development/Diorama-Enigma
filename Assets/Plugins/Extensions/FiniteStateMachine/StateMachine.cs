using System;
using System.Collections.Generic;
using Extensions.Log;

namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Базовая стейт-машина
    /// </summary>
    /// <remarks>
    /// Особенности стейтов:
    /// - Pause() — временная остановка без полного "dispose"
    /// - Resume() — возврат после Pause()
    /// - Exit() — гарантированный финальный cleanup, может вызываться из Stop() даже если стейт был paused
    /// </remarks>
    /// <typeparam name="TContext">Входные данные стейт-машины</typeparam>
    public abstract class StateMachine<TContext>
    {
        /// <summary>
        /// Событие изменения активного состояния машины
        /// </summary>
        /// <typeparam name="Type">Тип предыдущего состояния</typeparam>
        /// <typeparam name="Type">Тип нового состояния</typeparam>
        public event Action<Type, Type> onStateChanged;

        /// <summary>
        /// Состояние исполнения машины
        /// </summary>
        public bool IsRunning => currentState != null;

        /// <summary>
        /// Тип текущего исполняемого состояния
        /// </summary>
        public Type CurrentStateType => currentStateType;

        protected readonly IStateRegistry<TContext> registry;

        protected IState<TContext> currentState;
        protected Type currentStateType;

        protected bool logsEnabled = false;

        protected struct StackEntry
        {
            public Type StateType;
            public IState<TContext> State;
        }

        protected readonly Stack<StackEntry> stack = new Stack<StackEntry>();

        /// <summary>
        /// Новая стейт-машина
        /// </summary>
        /// <param name="registry">Реестр состояний</param>
        /// <exception cref="ArgumentNullException">Ошибка реестра</exception>
        protected StateMachine(IStateRegistry<TContext> registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        #region Манипуляция машиной

        /// <summary>
        /// Запуск машины с конкретного состояния
        /// </summary>
        /// <typeparam name="TInitialState">Тип входного состояния</typeparam>
        /// <param name="context">Входные данные стейт-машины</param>
        /// <exception cref="InvalidOperationException">Ошибка запуска машины</exception>
        public void Start<TInitialState>(TContext context)
            where TInitialState : class, IState<TContext>
        {
            if (currentState != null) throw new InvalidOperationException("Машина уже запущена");

            currentStateType = typeof(TInitialState);
            currentState = registry.Get(currentStateType);
            if (currentState == null) throw new InvalidOperationException($"Реестр вернул невалидное состояние ({currentStateType.Name})");

            currentState.Enter(context);
            if (logsEnabled) ServiceDebug.Log($"Запуск успешен");
        }

        /// <summary>
        /// Перезапуск стейт-машины
        /// </summary>
        /// <typeparam name="TInitialState">Тип входного состояния</typeparam>
        /// <param name="context">Входные данные стейт-машины</param>
        public void Restart<TInitialState>(TContext context)
            where TInitialState : class, IState<TContext>
        {
            if (logsEnabled) ServiceDebug.Log($"Перезапуск...");
            Stop(context);
            Start<TInitialState>(context);
        }

        /// <summary>
        /// Тик активного состояния машины
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        /// <exception cref="InvalidOperationException">Ошибка перехода состояния</exception>
        public void Tick(TContext context)
        {
            if (!IsRunning)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                ServiceDebug.LogWarning("Tick запрошен, но машина не запущена");
#endif
                return;
            }

            if (currentState == null) return;

            StateResult result = currentState.Tick(context);

            switch (result.Transition)
            {
                case StateTransition.Stay:
                    return;
                case StateTransition.Switch when result.NextStateType == null:
                    throw new InvalidOperationException("Switch требует валидный тип состояния для перехода");
                case StateTransition.Switch:
                    Switch(result.NextStateType, context);
                    return;
                case StateTransition.Push when result.NextStateType == null:
                    throw new InvalidOperationException("Push требует валидный тип состояния для перехода");
                case StateTransition.Push:
                    Push(result.NextStateType, context);
                    return;
                case StateTransition.Pop:
                    Pop(context);
                    return;
                case StateTransition.Stop:
                    Stop(context);
                    return;
                default:
                    throw new InvalidOperationException($"Необработанное состояние перехода ({result.Transition})");
            }
        }

        /// <summary>
        /// Остановка стейт-машины
        /// </summary>
        /// <param name="context">Входные данные стейт-машины</param>
        public void Stop(TContext context)
        {
            if (currentState != null)
            {
                currentState.Exit(context);
            }

            while (stack.Count > 0)
            {
                StackEntry entry = stack.Pop();
                if (entry.State == null) continue;
                if (ReferenceEquals(entry.State, currentState)) continue;

                if (entry.State is IStackableState<TContext> stackable)
                    stackable.ForceExit(context);
                else
                    entry.State.Exit(context);
            }

            stack.Clear();
            currentState = null;
            currentStateType = null;
            if (logsEnabled) ServiceDebug.Log($"Остановка успешна");
        }

        #endregion
        
        #region Логи

        /// <summary>
        /// Включить/отключить логи
        /// </summary>
        public void EnableLogs(bool isEnabled = true) => logsEnabled = isEnabled;
        
        #endregion
        
        #region Принудительное изменение состояния
        
        /// <summary>
        /// Принудительное изменение состояния машины
        /// </summary>
        /// <param name="context">Входные данные</param>
        /// <typeparam name="TState">Тип нового состояния</typeparam>
        public void GoTo<TState>(TContext context)
            where TState : class, IState<TContext>
        {
            GoTo(typeof(TState), context);
        }

        protected void GoTo(Type nextStateType, TContext context)
        {
            if (nextStateType == null) throw new ArgumentNullException(nameof(nextStateType));

            if (!typeof(IState<TContext>).IsAssignableFrom(nextStateType))
                throw new InvalidOperationException($"Тип '{nextStateType.Name}' не реализует IState");

            if (!IsRunning)
            {
                currentStateType = nextStateType;
                currentState = registry.Get(nextStateType);
                if (currentState == null) throw new InvalidOperationException($"Реестр вернул невалидное состояние ({nextStateType.Name})");

                currentState.Enter(context);
                onStateChanged?.Invoke(null, nextStateType);
                return;
            }

            if (currentStateType == nextStateType && stack.Count == 0)
                return;

            // важно: используем уже существующий Switch → не дублируем логику
            Switch(nextStateType, context);
        }
        
        #endregion
        
        #region Манипуляция состояниями

        protected void Switch(Type nextStateType, TContext context)
        {
            Type previous = currentStateType;

            while (stack.Count > 0)
            {
                StackEntry entry = stack.Pop();
                entry.State?.Exit(context);
            }

            currentState.Exit(context);

            currentStateType = nextStateType;
            currentState = registry.Get(nextStateType);
            if (currentState == null) throw new InvalidOperationException($"Реестр состояний вернул невалидное состояние для типа '{nextStateType.Name}'");

            currentState.Enter(context);

            onStateChanged?.Invoke(previous, nextStateType);
            if (logsEnabled) ServiceDebug.Log($"Сменено состояние с <b>{previous.Name}</b> на <b>{currentStateType.Name}</b>");
        }

        protected void Push(Type nextStateType, TContext context)
        {
            Type previous = currentStateType;

            stack.Push(new StackEntry { StateType = currentStateType, State = currentState });

            if (currentState is IStackableState<TContext> stackable)
            {
                stackable.Pause(context);
            }
            else
            {
                currentState.Exit(context);
            }

            currentStateType = nextStateType;
            currentState = registry.Get(nextStateType);
            if (currentState == null) throw new InvalidOperationException($"Реестр состояний вернул невалидное состояние для типа '{nextStateType.Name}'");

            currentState.Enter(context);

            onStateChanged?.Invoke(previous, nextStateType);
            if (logsEnabled) ServiceDebug.Log($"Сменено (Push) состояние с <b>{previous.Name}</b> на <b>{currentStateType.Name}</b>");
        }

        protected void Pop(TContext context)
        {
            if (stack.Count == 0)
            {
                throw new InvalidOperationException("Pop-переход запрошен, но стек состояний пуст");
            }

            Type previous = currentStateType;

            currentState.Exit(context);

            StackEntry entry = stack.Pop();

            currentStateType = entry.StateType;
            currentState = entry.State;
            if (currentState == null) throw new InvalidOperationException($"Стек содержит невалидное состояние для типа '{currentStateType?.Name}'");

            if (currentState is IStackableState<TContext> stackable)
            {
                stackable.Resume(context);
            }
            else
            {
                currentState.Enter(context);
            }

            onStateChanged?.Invoke(previous, currentStateType);
            if (logsEnabled) ServiceDebug.Log($"Сменено (Pop) состояние с <b>{previous.Name}</b> на <b>{currentStateType.Name}</b>");
        }

        #endregion
    }
}