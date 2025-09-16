namespace Core.UIStructure.Utilities
{
    public interface ISelectionListener<in TTarget>  where TTarget : ISelectionTarget
    {
        void OnSelectionChanged(TTarget prev, TTarget next);
    }
}