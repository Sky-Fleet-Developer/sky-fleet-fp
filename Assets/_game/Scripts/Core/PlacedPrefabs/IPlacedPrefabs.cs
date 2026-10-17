namespace Core.PlacedPrefabs
{
    /// <summary>
    /// Prefab instances placed over the terrain in tiles around the camera: their places are found on the GPU from the
    /// heights, then they are spawned as pooled prefab instances (rocks, trees...). Each kind has its own interface and
    /// settings on top of this.
    /// </summary>
    public interface IPlacedPrefabs
    {
        /// <summary>Instances are placed. Off, every placed instance is removed.</summary>
        bool Enabled { get; set; }

        /// <summary>Instances in the scene now.</summary>
        int Count { get; }
    }
}
