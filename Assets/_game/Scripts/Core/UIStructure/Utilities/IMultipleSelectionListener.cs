namespace Core.UIStructure.Utilities
{
    public interface IMultipleSelectionListener<in TTarget>  where TTarget : IMultipleSelectionTarget
    {
        void OnSelected(TTarget target);
        void OnDeselected(TTarget target);
        void OnFinalSelected(TTarget target);
    }
}