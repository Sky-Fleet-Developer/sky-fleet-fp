
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes.Indicators
{
    public class IndicatorBase<T> : DeviceBase<T>, IIndicator
    {
        public float ConvertRange(Vector2 inputRange, Vector2 outputRange, float value)
        {
            float r = (value - inputRange.x) / (inputRange.y - inputRange.x) * (outputRange.y - outputRange.x) + outputRange.x;
            return r;
        }
    }

    public class ArrowIndicator : IndicatorBase<float>
    {
        [SerializeField] protected Transform[] arrows;

        protected Quaternion GetRotateArrow(float startEuler, float angle)
        {
            return Quaternion.AngleAxis(startEuler + angle, Vector3.up);
        }
    }
}