using UnityEngine;

namespace Core.PlacedPrefabs
{
    /// <summary>
    /// The age window of a layer: the weight of its surface type in the alphamap says how old the grove is there (the
    /// denser the surface, the older), and a layer lies only in a range of ages. Full density inside the range,
    /// fading to nothing over the fade outside. The placement compute (PlacedPrefabPlacement.compute, PlacedAge)
    /// does the same.
    /// </summary>
    public static class PlacedAge
    {
        /// <summary>0..1 share of the density at an age (surface weight). 1 when the layer ignores the age.</summary>
        public static float Weight(IPlacedPrefabLayer layer, float age)
        {
            if (!layer.UseAge)
            {
                return 1;
            }

            return Weight(layer.AgeRange, layer.AgeFade, age);
        }

        public static float Weight(Vector2 range, float fade, float age)
        {
            fade = Mathf.Max(fade, 1e-3f);
            float rise = Mathf.Clamp01((age - (range.x - fade)) / fade);
            float fall = Mathf.Clamp01((range.y + fade - age) / fade);
            return rise * rise * (3 - 2 * rise) * (fall * fall * (3 - 2 * fall));
        }
    }
}
