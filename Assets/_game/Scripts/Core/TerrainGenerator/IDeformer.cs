using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator
{
    public interface IDeformer
    {
        List<DeformerLayerSetting> Settings { get; }

        Rect AxisAlinedRect { get; }

        Rect LocalAlinedRect { get; }
    }
}