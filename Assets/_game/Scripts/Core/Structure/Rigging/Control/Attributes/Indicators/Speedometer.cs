using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes.Indicators
{
    public class Speedometer : ArrowIndicator
    {
        [SerializeField] private float maxSpeed;

        [SerializeField] private float maxAngle;

        private float startSide;

        private void Start()
        {
            startSide = arrows[0].localEulerAngles.y;
        }

        public override void UpdateDevice()
        {
            float speed = wire.value;
            speed = Mathf.Min(maxSpeed, speed);
            float angle = ConvertRange(new Vector2(0, maxSpeed), new Vector2(0, maxAngle), speed);
            arrows[0].localRotation = GetRotateArrow(startSide, angle);
        }
    }
}