using System.Collections;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
using UnityEngine;

namespace Core.TerrainGenerator
{
    public interface IDeformer
    {
        List<IDeformerLayerSetting> Settings { get; }

        Rect AxisAlignedRect { get; }

        Vector4 LocalRect { get; }
        float Fade { get; }
    }
}