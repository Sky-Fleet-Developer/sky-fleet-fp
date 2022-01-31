using System;

namespace Core.Utilities
{
    public class LateEvent
    {
        public event Action action;
        public bool alredyInvoked { get; private set; }

        public void Subscribe(Action method)
        {
            if (alredyInvoked) method?.Invoke();
            else action += method;
        }

        public void Invoke()
        {
            alredyInvoked = true;
            action?.Invoke();
        }

        public void Reset()
        {
            alredyInvoked = false;
        }
    }
    public class LateEvent<T>
    {
        public event Action<T> action;
        public bool alredyInvoked { get; private set; }
        public T value;

        public void Subscribe(Action<T> method)
        {
            if (alredyInvoked) method?.Invoke(value);
            else action += method;
        }

        public void Invoke(T argument)
        {
            value = argument;
            alredyInvoked = true;
            action?.Invoke(argument);
        }

        public void Reset()
        {
            alredyInvoked = false;
        }
    }
    
    public class LateEvent<T1, T2>
    {
        public event Action<T1, T2> action;
        public bool alredyInvoked { get; private set; }
        public T1 value1;
        public T2 value2;

        public void Subscribe(Action<T1, T2> method)
        {
            if (alredyInvoked) method?.Invoke(value1, value2);
            else action += method;
        }

        public void Invoke(T1 t1, T2 t2)
        {
            value1 = t1;
            value2 = t2;
            alredyInvoked = true;
            action?.Invoke(t1, t2);
        }

        public void Reset()
        {
            alredyInvoked = false;
        }
    }
}
