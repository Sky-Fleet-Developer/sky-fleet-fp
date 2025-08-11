using Core.Configurations;
using UnityEngine;

namespace Core.Structure.Rigging.Cargo
{
    public interface ICargoTrunk : IBlock
    {
        bool TryPlaceCargo(ITablePrefab cargo, Vector3Int position, out PlaceCargoHandler handler);
    }

    public interface ICargoTrunkPlayerInterface : ICargoTrunk
    {
        void EnterPlacement(ITablePrefab cargo);
        void MoveTo(Vector3Int position);
        void ExitPlacement();
    }
}