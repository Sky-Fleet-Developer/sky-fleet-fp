using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class AltitudeIndicator : MonoBehaviour, IDevice
    {
        public IStructure Structure => structure;
        public IBlock Block => block;

        [SerializeField] private Transform bubble_pitch;
        [SerializeField] private Transform bubble_roll;
        [SerializeField] private Transform compass;

        protected IStructure structure;
        protected IBlock block;
        
        public void Init(IStructure structure, IBlock block)
        {
        }

        public void UpdateDevice()
        {
            float pitch = -Mathf.Asin(transform.forward.y) * Mathf.Rad2Deg;
            float roll = -Mathf.Atan2(transform.right.y, transform.up.y) * Mathf.Rad2Deg;
            bubble_pitch.localRotation = Quaternion.Euler(pitch, 0, 0);
            bubble_roll.localRotation = Quaternion.Euler(0, 0, roll);
            compass.localEulerAngles = new Vector3(0, -transform.eulerAngles.y, 0);
        }
    }
}