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
        void RegisterDisposeListener(IWorldEntityDisposeListener listener);
        void UnregisterDisposeListener(IWorldEntityDisposeListener listener);
    }
}