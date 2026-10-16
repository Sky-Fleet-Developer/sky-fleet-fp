namespace Core.Rocks
{
    /// <summary>
    /// Large rocks with collision, placed over the terrain in tiles around the camera: their places are found on the
    /// GPU from the heights, then they are spawned as pooled prefab instances. Hand-placed rocks are plain prefabs in
    /// the scene and don't go through it.
    /// </summary>
    public interface IRocks
    {
        /// <summary>Rocks are placed. Off, every placed rock is removed.</summary>
        bool Enabled { get; set; }

        RockSettings Settings { get; }

        /// <summary>Rock instances in the scene now.</summary>
        int Count { get; }
    }
}
