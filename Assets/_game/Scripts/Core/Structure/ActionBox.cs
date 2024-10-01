using System;

namespace Core.Structure
{
    public class ActionBox
    {
    }
    
    public class ActionBox<T> : ActionBox
    {
        private Action<T> action;

        public ActionBox(Action<T> value)
        {
            action = value;
        }

        public void Call(T value)
        {
            action?.Invoke(value);
        }
    }
}