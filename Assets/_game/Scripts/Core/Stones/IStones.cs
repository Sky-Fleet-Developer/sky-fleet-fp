namespace Core.Stones
{
    /// <summary>
    /// Small stones (pebbles up to knee-high rocks, without collision) scattered over the terrain on the GPU around the
    /// main camera. Areas without stones are the areas of Core.Scatter.IScatter, shared with the other scattered details.
    /// </summary>
    public interface IStones
    {
        /// <summary>Stones are drawn.</summary>
        bool Enabled { get; set; }

        StoneSettings Settings { get; }
    }
}
