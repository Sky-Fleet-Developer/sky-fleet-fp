using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Core.Structure.Rigging.Control.Attributes.Indicators
{
    public interface IIndicator : IDevice
    {
        float ConvertRange(Vector2 inputRange, Vector2 outputRange, float value);
    }
}