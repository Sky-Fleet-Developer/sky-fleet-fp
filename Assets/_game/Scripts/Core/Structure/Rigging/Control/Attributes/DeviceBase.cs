using System.Collections.Generic;
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
        
        public IStructure Structure => structure;
        public IBlock Block => block;

        protected IStructure structure;
        protected IBlock block;

        public virtual void Init(IStructure structure, IBlock block)
        {
            this.structure = structure;
            this.block = block;
        }

        public virtual void UpdateDevice()
        {
        }
    }

    public abstract class DeviceBase<T> : DeviceBase
    {
        public Port<T> port = new Port<T>();
    }
}
