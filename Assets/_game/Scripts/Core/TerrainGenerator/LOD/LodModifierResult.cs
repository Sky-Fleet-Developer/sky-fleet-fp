namespace Core.TerrainGenerator.LOD
{
    public struct LodModifierResult
    {
        /// <summary>Скорректированная дистанция. По умолчанию = rawDistance (без изменений).</summary>
        public float AdjustedDistance;

        /// <summary>Принудительная минимальная глубина дробления. 0 = не форсировать.</summary>
        public int MinDepth;

        public static LodModifierResult None(float rawDistance) =>
            new LodModifierResult { AdjustedDistance = rawDistance, MinDepth = 0 };
    }
}