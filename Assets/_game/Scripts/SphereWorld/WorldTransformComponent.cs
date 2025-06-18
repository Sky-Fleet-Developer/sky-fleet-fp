using System;
using UnityEngine;

namespace SphereWorld
{
    [RequireComponent(typeof(WorldEntity))]
    public class WorldTransformComponent : MonoBehaviour
    {
        private WorldEntity _entity;

        private void Awake()
        {
            _entity = GetComponent<WorldEntity>();
            _entity.OnEntityBecameVisible += OnEntityBecameVisible;
        }

        private void OnEntityBecameVisible()
        {
            transform.position = _entity.GetOffset();
        }
    }
}