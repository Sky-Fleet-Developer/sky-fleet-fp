using System;
using Core.Items;
using Core.World;
using UnityEngine;
using Zenject;

namespace Runtime.Content
{
    //[Serializable]
    public class TransportSpawnBehaviour : ITransportSpawnBehaviour
    {
        [Inject] private WorldSpace _worldSpace;
        
        public UnitEntity Spawn(EntityObjectInstaller source, Vector3 position, Quaternion rotation)
        {
            var entity = new UnitEntity(source.itemDescription, position, rotation);
            _worldSpace.AddEntity(entity);
            
            return entity;
        }
        
        private bool IsEntityShouldBeLanded(ref Vector3 position, Vector3 size)
        {
            if (Physics.Raycast(position, Vector3.down, out var hit, 100f))
            {
                position = hit.point + size.y * Vector3.up;
                return true;
            }

            return false;
        }
    }
}