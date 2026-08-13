using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.World
{
    public interface IWorldEntity : IDisposable
    {
        protected static int IdCounter = 0;
        public int Id { get; }
        Vector3 SpacePosition { get; }
        Vector3 WorldPosition => WorldOffset.SpaceToWorld(SpacePosition);
        void OnLodChanged(int lod);
        Task GetAnyLoad();
        void Initialize();
        void RegisterDisposeListener(IWorldEntityDisposeListener listener);
        void UnregisterDisposeListener(IWorldEntityDisposeListener listener);
    }

    public interface IObjectEntity : IWorldEntity
    {
        GameObject GameObject { get; }
    }
}