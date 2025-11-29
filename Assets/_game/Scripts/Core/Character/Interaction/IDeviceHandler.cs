using Core.Structure.Rigging;

namespace Core.Character.Interaction
{
    public interface IDeviceHandler : ICharacterHandler
    {
        void MoveValueInteractive(float val);
        void ExitControl();
        IDriveInterface Block { get; }
    }
}