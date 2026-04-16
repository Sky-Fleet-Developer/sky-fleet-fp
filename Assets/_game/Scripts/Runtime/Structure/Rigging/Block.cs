using System.Collections.Generic;
using Core.Structure;
using Core.Utilities;
using Runtime.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging
{

    public abstract class Block : ItemObject, IBlock
    {
        public Vector3 localPosition => transform.localPosition;
        public Parent Parent { get; private set; }
        public IStructure Structure { get; private set; }

        private Bounds boundsCash;

        [ShowInInspector]
        public string MountingType
        {
            get => mountingType;
            set => mountingType = value;
        }

        public bool IsActive => gameObject && gameObject.activeSelf && enabled;

        public virtual float Mass { get => _mass; }
        public virtual Vector3 LocalCenterOfMass => Vector3.zero;
        [SerializeField, HideInInspector] private string mountingType;
        private float _mass;

        public virtual void InitBlock(IStructure structure, Parent parent)
        {
            Parent = parent;
            Structure = structure;
            boundsCash = transform.GetBounds();
            boundsCash.center = parent.Transform.InverseTransformPoint(boundsCash.center);
        }

        protected override void OnItemSet()
        {
            _mass = SourceItem.GetMass();
        }

        public void Remove()
        {
            Structure = null;
        }

        private void Start()
        {
        }

        public Bounds GetBounds()
        {
            return boundsCash;
        }

#if UNITY_EDITOR
        void IBlock.SetStructureEditor(IStructure structure)
        {
            Structure = structure;
        }  
#endif
    }
}
