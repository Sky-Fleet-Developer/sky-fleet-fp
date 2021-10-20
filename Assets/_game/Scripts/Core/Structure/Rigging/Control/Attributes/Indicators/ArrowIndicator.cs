using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes.Indicators
{
    public class ArrowIndicator : IndicatorDependet<float>
    {
        [System.Serializable]
        public class ArrowSetting
        {
            public Transform arrow;
            public Vector2 minMaxAngle;
            public float multiple;
        }

        [SerializeField] private ArrowSetting[] arrows;

        public override void UpdateDevice()
        {
            float value = wire.value;
            for (int i = 0; i < arrows.Length; i++)
            {
                float currValue = value * arrows[i].multiple;
                float angle = arrows[i].minMaxAngle.y - arrows[i].minMaxAngle.x;
                angle *= currValue;
                arrows[i].arrow.transform.localRotation = GetRotateArrow(arrows[i].minMaxAngle.x, angle);
            }
        }
    }
}