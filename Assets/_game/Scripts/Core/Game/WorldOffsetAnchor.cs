using System;
using UnityEngine;

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

        private void OnOffsetChange(Vector3 offset)
        {
            transform.position += offset;
            Debug.Log($"WORLD_OFFSET_ANCHOR: {name} moved to {transform.position}");
        }
    }

    public static class MonoBehaviourExtension
    {
        public static void AddWorldOffsetAnchor(this MonoBehaviour target)
        {
            if (!target.TryGetComponent(out WorldOffsetAnchor _)) target.gameObject.AddComponent<WorldOffsetAnchor>();
        }
    }
}
