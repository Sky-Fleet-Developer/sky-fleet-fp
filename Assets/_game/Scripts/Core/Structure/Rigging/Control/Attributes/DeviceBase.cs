using System;
using System.Collections.Generic;
using System.Text;
using Core.Character;
using Core.Character.Interaction;
using Core.Graph;
using Core.Graph.Wires;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Structure.Rigging.Control.Attributes
{

    public abstract class DeviceBase : MonoBehaviour, IDevice
    {
        [ShowInInspector]
        public string AssetId
        {
            get
            {
                if (string.IsNullOrEmpty(assetId))
                {
                    var sb = new StringBuilder(name);
                    for (int i = 0; i < sb.Length; i++)
                    {
                        if (char.IsUpper(sb[i]))
                        {
                            sb.Insert(i++, "-");
                            sb[i] = char.ToLower(sb[i]);
                        }
                    }
                    assetId = sb.ToString();
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);   
#endif
                }
                return assetId;
            }
            set => assetId = value;
        }
        [FormerlySerializedAs("guid")] [SerializeField, HideInInspector] private string assetId;

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
