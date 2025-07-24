using System;

namespace Core.Patterns.State
{
    public interface IStateMaster
    {
        IState CurrentState { get; set; }
        event Action StateChanged;
    }

    public interface IState
    {
        void Run();
    }

    public interface IState<out T> : IState where T : IStateMaster
    {
        public T Master { get; }
    }
}
