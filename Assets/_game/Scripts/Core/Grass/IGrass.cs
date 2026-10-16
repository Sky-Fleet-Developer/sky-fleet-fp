namespace Core.Grass
{
    /// <summary>Grass on the terrain, placed and drawn on the GPU around the main camera.</summary>
    public interface IGrass
    {
        /// <summary>Grass is drawn. Off, the terrain loses the far grass color too.</summary>
        bool Enabled { get; set; }

        GrassSettings Settings { get; }
    }
}
