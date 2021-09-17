using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Structure.Rigging.Control.Attributes
{

    public interface IDevice
    {
        IStructure Structure { get; }
        IBlock Block { get; }
        string Port { get; set; }
        void Init(IStructure structure, IBlock block, string port);
        void UpdateDevice();
    }

    public interface IArrowDevice : IDevice
    {
        
    }
}
