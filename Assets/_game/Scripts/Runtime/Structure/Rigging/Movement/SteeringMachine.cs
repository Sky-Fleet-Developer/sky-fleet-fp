using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Wires;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;


namespace Runtime.Structure.Rigging.Movement
{
    public class SteeringMachine : Block, IUpdatableBlock
    {
        public Port<float> value = new Port<float>(PortType.Thrust);

        [SerializeField] private Vector2 limitRotate;

        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [ShowInInspector]
        Transform Target
        {
            set
            {
                path = Factory.GetPath(value);
            }
        }

        [SerializeField] string path;

        Transform target;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            string[] names = path.Split('/');
            Transform transformCurrent = structure.transform;
            foreach (string name in names)
            {
                transformCurrent = transformCurrent.Find(name);
                if(transformCurrent == null)
                {
                    Debug.LogError("No find transform.");
                }
            }
            target = transformCurrent;
        }

        public void UpdateBlock(int lod)
        {
            float angleRotate = Mathf.Lerp(limitRotate.x, limitRotate.y, Mathf.Sign(value.Value));
            target.localRotation = Quaternion.AngleAxis(angleRotate * value.Value, rotationAxis);
        }
    }
}