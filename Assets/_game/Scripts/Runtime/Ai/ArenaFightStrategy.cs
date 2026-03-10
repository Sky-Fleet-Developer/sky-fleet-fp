using System;
using System.Collections.Generic;
using Core.Ai;
using Core.World;
using UnityEngine;

namespace Runtime.Ai
{
    public class ArenaFightStrategy : MonoBehaviour, IAiStrategy, IWorldEntityDisposeListener
    {
        private List<ItemEntity> _controllableEntities = new ();
        
        public void AddControllableEntity(ItemEntity entity)
        {
            _controllableEntities.Add(entity);
            entity.RegisterDisposeListener(this);
        }

        public void RemoveControllableEntity(ItemEntity entity)
        {
            _controllableEntities.Remove(entity);
        }

        public void OnEntityDisposed(IWorldEntity entity)
        {
            RemoveControllableEntity(entity as ItemEntity);
        }

        private void Update()
        {
            
        }
    }
}