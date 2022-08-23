using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    public interface IColorFilter
    {
        Color Evaluate(Color reference);
    }
}
