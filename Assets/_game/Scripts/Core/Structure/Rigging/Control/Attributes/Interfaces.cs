namespace Core.Structure.Rigging.Control.Attributes
{

    public interface IDevice
    {
        IStructure Structure { get; }
        IBlock Block { get; }
        string Port { get; set; }
        void Init(IStructure structure, IBlock block, string port);
        void UpdateDevice(int load);
    }

    public interface IArrowDevice : IDevice
    {
        
    }
}
