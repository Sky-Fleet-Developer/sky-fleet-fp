namespace Core.Trees
{
    /// <summary>
    /// The far ring of trees: beyond the distance where the models of the trees are culled, every tree is drawn as an
    /// impostor (<see cref="TreeImpostor"/>) up to the far range of the <see cref="TreeSettings"/>. It places the
    /// trees the same way as <see cref="ITrees"/> does, so a tree stands in the same place in both rings.
    /// </summary>
    public interface ITreeFarField
    {
        /// <summary>The impostors are drawn.</summary>
        bool Enabled { get; set; }
    }
}
