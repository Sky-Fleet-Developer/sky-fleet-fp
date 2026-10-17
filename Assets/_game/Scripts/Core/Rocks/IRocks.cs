using Core.PlacedPrefabs;

namespace Core.Rocks
{
    /// <summary>
    /// Large rocks with collision, placed over the terrain in tiles around the camera. Hand-placed rocks are plain
    /// prefabs in the scene and don't go through it.
    /// </summary>
    public interface IRocks : IPlacedPrefabs
    {
        RockSettings Settings { get; }
    }
}
