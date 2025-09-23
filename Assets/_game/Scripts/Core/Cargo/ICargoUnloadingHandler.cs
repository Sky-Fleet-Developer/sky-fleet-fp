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
        void BeginPlacement(ITablePrefab cargo);
        void EndPlacement();
    }
    
    public interface ICargoUnloadingHandler : ICharacterHandler
    {
        IEnumerable<ITablePrefab> AvailableCargo { get; }
        bool TryUnload(ITablePrefab cargo, Vector3 targetGroundPoint, Quaternion targetRotation, out PlaceCargoHandler handler);
        void Detach(ITablePrefab cargo);
    }
}