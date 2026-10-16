namespace Core.Grass
{
    /// <summary>Grass on the terrain, placed and drawn on the GPU around the main camera.</summary>
    public interface IGrass
    {
        /// <summary>Grass is drawn. Off, the terrain loses the far grass color too.</summary>
        bool Enabled { get; set; }

        GrassSettings Settings { get; }

        /// <summary>Keeps the grass out of an area until it is removed. Adding twice adds once.</summary>
        void AddArea(IGrassArea area);

        void RemoveArea(IGrassArea area);

        /// <summary>The grass bends away from the pusher until it is removed. Adding twice adds once.</summary>
        void AddPusher(IGrassPusher pusher);

        void RemovePusher(IGrassPusher pusher);
    }
}
