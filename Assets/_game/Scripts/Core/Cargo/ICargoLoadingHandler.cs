using System.Collections.Generic;
using Cinemachine;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Structure.Rigging.Cargo;
using UnityEngine;

namespace Core.Cargo
{
    public interface ICargoLoadingPlayerHandler : ICargoLoadingHandler
    {
        void Enter();
        void Exit();
    }
    public interface ICargoLoadingHandler : ICharacterHandler
    {
        IEnumerable<ITablePrefab> AvailableCargo { get; }
        IEnumerable<ICargoTrunk> AvailableTrunks { get; }

        bool TryLoad(ITablePrefab cargo, ICargoTrunk trunk, Vector3Int position);
    }
}