using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.World
{
    public interface IWorldEntity : IDisposable
    {
        Vector3 Position { get; }
        void OnLodChanged(int lod);
        Task GetAnyLoad();
        void Initialize();
        void RegisterDisposeListener(IWorldEntityDisposeListener listener);
        void UnregisterDisposeListener(IWorldEntityDisposeListener listener);
    }

    public interface IObjectEntity : IWorldEntity
    {
        GameObject GameObject { get; }
        void UpdateTransforms();
    }
}