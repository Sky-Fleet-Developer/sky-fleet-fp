using System;
using UnityEngine;

namespace Core.Game
{
    public class WorldOffsetAnchor : MonoBehaviour
    {
        public void Awake()
        {
            WorldOffset.OnWorldOffsetChange += OnOffsetChange;
        }

        private void OnOffsetChange(Vector3 offset)
        {
            transform.position += offset;
        }
    }
}
