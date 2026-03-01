using Core.Configurations;
using UnityEngine;

namespace Core.Structure.Rigging.Cargo
{
    public interface ICargoTrunk : IBlock
    {
        bool TryPlaceCargo(IRemotePrefab cargo, Vector3Int position, out PlaceCargoHandler handler);
    }

    public interface ICargoTrunkPlayerInterface : ICargoTrunk
    {
        void EnterPlacement(IRemotePrefab cargo);
        void Move(Vector3Int delta);
        void ExitPlacement();
        bool Confirm();
    }
}