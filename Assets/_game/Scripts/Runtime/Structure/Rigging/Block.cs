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
        public Vector3 localPosition => transform.position;
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
    }
}
