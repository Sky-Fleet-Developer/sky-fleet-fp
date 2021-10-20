using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class ArrowIndicator : DeviceBase<float>
    {
        public Vector3 arrowAxe = Vector3.up;
        [System.Serializable]
        public class ArrowSetting
        {
            public Transform arrow;
            public Vector2 minMaxAngle;
            public float multiple;

            public Vector3 startAngle { get; private set; }

            public void Init()
            {
                startAngle = arrow.localEulerAngles;
            }
        }

        [SerializeField] private ArrowSetting[] arrows;

        public override void UpdateDevice()
        {
            float value = port.Value;
            for (int i = 0; i < arrows.Length; i++)
            {
                float currValue = value * arrows[i].multiple;
                float angle = arrows[i].minMaxAngle.y - arrows[i].minMaxAngle.x;
                angle *= currValue;
                arrows[i].arrow.transform.localEulerAngles = arrows[i].startAngle + arrowAxe * angle;
            }
        }
    }
}