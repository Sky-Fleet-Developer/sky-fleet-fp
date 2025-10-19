using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.World
{
    public interface IWorldEntity
    {
        Vector3 Position { get; }
        void OnLodChanged(int lod);
        Task GetAnyLoad();
        Task Serialize(Stream stream);
        Task Deserialize(Stream stream);
    }
}