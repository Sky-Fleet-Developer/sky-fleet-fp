using System.Collections;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
using UnityEngine;

namespace Core.TerrainGenerator
{
    public interface IDeformer
    {
        List<IDeformerLayerSetting> Settings { get; }

        Rect AxisAlinedRect { get; }

        Rect LocalAlinedRect { get; }
    }
}