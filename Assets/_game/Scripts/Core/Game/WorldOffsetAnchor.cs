using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Game
{
    public class WorldOffsetAnchor : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        public void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            WorldOffset.OnWorldOffsetChange += OnOffsetChange;
        }

        private void OnDestroy()
        {
            WorldOffset.OnWorldOffsetChange -= OnOffsetChange;
        }

        private void OnOffsetChange(Vector3 offset)
        {
            Vector3 prev = transform.position;
            if (_rigidbody)
            {
                _rigidbody.position += offset;
                transform.position = _rigidbody.position;
            }
            else
            {
                transform.position += offset;
            }

            Debug.Log($"WORLD_OFFSET_ANCHOR: {name} moved from {prev} to {transform.position}");
        }
    }

    public static class Extension
    {
        public static void AddWorldOffsetAnchor(this Component target)
        {
            if (!target.TryGetComponent(out WorldOffsetAnchor _)) target.gameObject.AddComponent<WorldOffsetAnchor>();
        }
        public static void RemoveWorldOffsetAnchor(this Component target)
        {
            if (target.TryGetComponent(out WorldOffsetAnchor worldOffsetAnchor))
            {
                Object.Destroy(worldOffsetAnchor);
            } 
        }
        public static void AddWorldOffsetAnchor(this GameObject target)
        {
            if (!target.TryGetComponent(out WorldOffsetAnchor _)) target.AddComponent<WorldOffsetAnchor>();
        }
    }
}
