using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Core.Graph.Wires
{
    [ShowInInspector, InlineProperty(LabelWidth = 150)]
    public class ActionPort<T> : Port
    {
        private Action<T> pushStartAction;

        public override void SetWire(Wire wire)
        {
            base.SetWire(wire);
            if (pushStartAction != null)
            {
                ((ActionWire<T>)wire).RegisterActions(pushStartAction);
            }
        }

        public void AddRegisterAction(Action<T> action)
        {
            if (wire == null)
            {
                pushStartAction += action;
            }
            else
            {
                ((ActionWire<T>)wire).RegisterActions(action);
            }
        }

        public override bool CanConnect(Port port)
        {
            return port is ActionPort<T>;
        }

        public void Call(T param)
        {
            ((ActionWire<T>)wire).CallActions(param);
        }

        public override Wire CreateWire()
        {
            return new ActionWire<T>();
        }

        public override string ToString()
        {
            return "Action";
        }
    }

    [ShowInInspector, InlineProperty(LabelWidth = 150)]
    public class ActionPort : Port
    {
        private Action pushStartAction;

        public override void SetWire(Wire wire)
        {
            base.SetWire(wire);
            if (pushStartAction != null)
            {
                ((ActionWire)wire).RegisterActions(pushStartAction);
            }
        }

        public void AddRegisterAction(Action action)
        {
            if (wire == null)
            {
                pushStartAction += action;
            }
            else
            {
                ((ActionWire)wire).RegisterActions(action);
            }
        }

        public override bool CanConnect(Port port)
        {
            return port is ActionPort;
        }

        public void Call()
        {
            ((ActionWire)wire).CallActions();
        }

        public override Wire CreateWire()
        {
            return new ActionWire();
        }

        public override string ToString()
        {
            return "Action";
        }
    }

    [ShowInInspector]
    class ActionWire : Wire
    {
        private List<Action> registersAction = new List<Action>();

        public override bool CanConnect(PortPointer port)
        {
            return port.Port is ActionPort;
        }

        public void RegisterActions(Action action)
        {
            registersAction.Add(action);
        }

        public void CallActions()
        {
            for (int i = 0; i < registersAction.Count; i++)
            {
                registersAction[i]();
            }
        }
    }

    [ShowInInspector]
    class ActionWire<T> : Wire
    {
        private List<Action<T>> registersAction = new List<Action<T>>();

        public override bool CanConnect(PortPointer port)
        {
            return port.Port is ActionPort<T>;
        }

        public void RegisterActions(Action<T> action)
        {
            registersAction.Add(action);
        }

        public void CallActions(T param)
        {
            for (int i = 0; i < registersAction.Count; i++)
            {
                registersAction[i](param);
            }
        }
    }
}