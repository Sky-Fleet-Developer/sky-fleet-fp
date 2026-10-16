namespace Core.Grass
{
    /// <summary>
    /// Grass on the terrain, placed and drawn on the GPU around the main camera. Areas without grass are the areas of
    /// Core.Scatter.IScatter, shared with the other scattered details.
    /// </summary>
    public interface IGrass
    {
        /// <summary>Grass is drawn. Off, the terrain loses the far grass color too.</summary>
        bool Enabled { get; set; }

        GrassSettings Settings { get; }

        /// <summary>The grass bends away from the pusher until it is removed. Adding twice adds once.</summary>
        void AddPusher(IGrassPusher pusher);

        void RemovePusher(IGrassPusher pusher);
    }
}
