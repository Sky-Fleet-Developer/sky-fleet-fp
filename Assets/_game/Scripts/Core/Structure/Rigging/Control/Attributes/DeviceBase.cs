using System;
using System.Collections.Generic;
using Core.Character;
using Core.Character.Interaction;
using Core.Graph;
using Core.Graph.Wires;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes
{

    public abstract class DeviceBase : MonoBehaviour, IDevice
    {
        [ShowInInspector]
        public string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(guid))
                {
                    guid = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);   
#endif
                }
                return guid;
            }
            set => guid = value;
        }
        [SerializeField, HideInInspector] private string guid;

        public List<string> Tags => tags;
        [SerializeField] private List<string> tags;

        public IGraph Graph => _graph;
        public IBlock Block => _block;

        private IGraph _graph;
        private IBlock _block;

        public virtual void Init(IGraph graph, IBlock block)
        {
            _graph = graph;
            _block = block;
        }

        public virtual void UpdateDevice()
        {
        }
    }

    public abstract class DeviceBase<T> : DeviceBase, IDeviceWithPort, IDeviceHandler where T : Port
    {
        public abstract void MoveValueInteractive(float val);
        public abstract void ExitControl();
        IDriveInterface IDeviceHandler.Block => (IDriveInterface)base.Block;
        public abstract T Port { get; }
        Port IPortUser.GetPort() => Port;
        string IPortUser.GetName() => name;
        public void Interact(InteractEventData data)
        {
            if (data.used)
            {
                return;
            }
            data.Controller.EnterHandler(this);
            data.Use();
        }
    }
}
