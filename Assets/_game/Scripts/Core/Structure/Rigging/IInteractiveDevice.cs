namespace Core.Structure.Rigging
{
    public interface IInteractiveDevice : IInteractiveObject
    {
        void MoveValueInteractive(float val);
        void ExitControl();
        IDriveInterface Block { get; }
    }
}