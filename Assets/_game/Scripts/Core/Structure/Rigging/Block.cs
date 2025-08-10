using System.Collections.Generic;
using Core.Configurations;
using Core.Trading;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Rigging
{

    public abstract class Block : MonoBehaviour, IBlock
    {
        public Vector3 localPosition => transform.position;
        public Parent Parent { get; private set; }
        public IStructure Structure { get; private set; }

        private Bounds boundsCash;

        [ShowInInspector] public string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(guid)) guid = System.Guid.NewGuid().ToString();
                return guid;
            }
            set => guid = value;
        }
        [SerializeField, HideInInspector] private string guid;
        public List<string> Tags => tags;
        [SerializeField] private List<string> tags;
        
        [ShowInInspector]
        public string MountingType
        {
            get => mountingType;
            set => mountingType = value;
        }

        public bool IsActive => gameObject && gameObject.activeSelf && enabled;

        public virtual float Mass { get => mass; }
        public virtual Vector3 LocalCenterOfMass => Vector3.zero;

        [SerializeField]
        private float mass = 10;

        [SerializeField, HideInInspector] private string mountingType;

        public virtual void InitBlock(IStructure structure, Parent parent)
        {
            Parent = parent;
            Structure = structure;
            boundsCash = transform.GetBounds();
            boundsCash.center = parent.Transform.InverseTransformPoint(boundsCash.center);
        }

        private void Start()
        {
        }

        public Bounds GetBounds()
        {
            return boundsCash;
        }

        private ItemSign _sourceItem;
        ItemSign IItemInstance.SourceItem => _sourceItem;
        void IItemInstanceHandle.SetSourceItem(ItemSign sign)
        {
            _sourceItem = sign;
        }
    }
}
