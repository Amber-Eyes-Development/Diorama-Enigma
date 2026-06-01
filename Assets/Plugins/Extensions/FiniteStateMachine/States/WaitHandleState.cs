using System;

namespace Extensions.FiniteStateMachine
{
    /// <summary>
    /// Абстракция состояния ожидания
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public abstract class WaitHandleState<TContext, TNextState> : BaseState<TContext>
        where TNextState : class, IState<TContext>
    {
        protected IWaitHandle waitHandle;

        public override void Enter(TContext context)
        {
            if (waitHandle != null)
            {
                waitHandle.Cancel();
                waitHandle = null;
            }

            waitHandle = CreateWaitHandle(context);
        }

        public override StateResult Tick(TContext context)
        {
            if (waitHandle == null || waitHandle.IsDone)
                return StateResult.Switch<TNextState>();

            return StateResult.Stay();
        }

        public override void Exit(TContext context)
        {
            if (waitHandle != null)
            {
                waitHandle.Cancel();
            }

            waitHandle = null;
        }

        protected abstract IWaitHandle CreateWaitHandle(TContext context);
    }
}