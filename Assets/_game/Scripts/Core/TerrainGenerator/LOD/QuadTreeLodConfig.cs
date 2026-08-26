using System;

namespace Core.TerrainGenerator.LOD
{
    /// <summary>
    /// Настройки LOD: дистанции, при которых узел данной глубины ещё должен дробиться дальше.
    /// LodDistances[0] — порог для корня (глубина 0), и т.д.
    /// Чем меньше индекс — тем больше threshold (близкие к камере зоны дробятся сильнее всего).
    /// </summary>
    [Serializable]
    public class QuadTreeLodConfig
    {
        public float[] LodDistances;
        public int MaxDepth => LodDistances.Length;
    }
}