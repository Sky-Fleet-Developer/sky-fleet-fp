using System;
using System.Collections.Generic;
using Core.Character;
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

        public IGraphHandler Graph => _graph;
        public IBlock Block => _block;

        private IGraphHandler _graph;
        private IBlock _block;

        public virtual void Init(IGraphHandler graph, IBlock block)
        {
            _graph = graph;
            _block = block;
        }

        public virtual void UpdateDevice()
        {
        }
    }

    public abstract class DeviceBase<T> : DeviceBase, IDeviceWithPort where T : Port
    {
        public abstract void MoveValueInteractive(float val);
        public abstract void ExitControl();

        IInteractiveBlock IInteractiveDevice.Block => (IInteractiveBlock)base.Block;
        public abstract T Port { get; }
        Port IPortUser.GetPort() => Port;
        string IPortUser.GetName() => name;
        public virtual bool EnableInteraction => true;
        public Transform Root => transform;
        public virtual bool RequestInteractive(ICharacterController character, out string data)
        {
            data = string.Empty;
            return true;
        }
    }
}
