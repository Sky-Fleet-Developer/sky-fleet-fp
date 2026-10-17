using Core.PlacedPrefabs;

namespace Core.Trees
{
    /// <summary>
    /// Trees with collision, placed over the terrain in tiles around the camera. Hand-placed trees are plain prefabs
    /// in the scene and don't go through it.
    /// </summary>
    public interface ITrees : IPlacedPrefabs
    {
        TreeSettings Settings { get; }
    }
}
