using Core.Structure;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class Rotator : AbstractRotator
    {
        [SerializeField][ConstantField]
        private string targetParent;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        private Parent _targetParent;
        [ShowInInspector]
        private Transform TargetParent
        {
            get => this.GetParentByPath(ref _targetParent, targetParent)?.Transform;
            set
            {
                this.SetParentByPath(value, ref targetParent);
                _targetParent = null;
            }
        }
        private Quaternion _initialRotation;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            rotationAxis.Normalize();
            _initialRotation = TargetParent.localRotation;
        }

        protected override Vector3 GetRotationAxis()
        {
            return rotationAxis;
        }

        protected override float TryApplyRotation(float angle)
        {
            TargetParent.localRotation = _initialRotation * Quaternion.AngleAxis(angle, rotationAxis);
            return angle;
        }
    }
}
