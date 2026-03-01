using System.Collections.Generic;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Structure.Rigging.Cargo;
using UnityEngine;

namespace Core.Cargo
{
    public interface ICargoUnloadingPlayerHandler : ICargoUnloadingHandler
    {
        void Enter();
        void Exit();
        void BeginPlacement(IRemotePrefab cargo);
        void EndPlacement();
    }
    
    public interface ICargoUnloadingHandler : ICharacterHandler
    {
        IEnumerable<IRemotePrefab> AvailableCargo { get; }
        bool TryUnload(IRemotePrefab cargo, Vector3 targetGroundPoint, Quaternion targetRotation, out PlaceCargoHandler handler);
        void Detach(IRemotePrefab cargo);
    }
}