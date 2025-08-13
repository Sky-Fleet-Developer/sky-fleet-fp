using System.Collections.Generic;
using Core.Utilities;
using UnityEngine;

namespace Runtime.Cargo
{
    public class CargoVolumeView : MonoBehaviour
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        [SerializeField] private Transform cube;
        [SerializeField] private Color[] colorsMap;
        private readonly List<Transform> views = new();
        private readonly Dictionary<Vector3Int, Material> materials = new();
        private float _particleSize;

        private void Awake()
        {
            DynamicPool.Instance.Return(cube);
        }

        public void SetVolume(IReadOnlyList<Vector3Int> volume, Vector3Int position, float particleSize)
        {
            _particleSize = particleSize;
            for (var i = 0; i < volume.Count; i++)
            {
                if (views.Count == i)
                {
                    views.Add(DynamicPool.Instance.Get(cube, transform));
                }
                
                materials[volume[i]] = views[i].GetComponent<Renderer>().material;
                views[i].localPosition = (Vector3)(volume[i] + position) * particleSize;
                views[i].localScale = Vector3.one * particleSize;
            }

            for (int i = volume.Count; i < views.Count; i++)
            {
                DynamicPool.Instance.Return(views[i]);
            }
            views.RemoveRange(volume.Count, views.Count - volume.Count);
        }

        public void SetCollisionMask(IEnumerable<(Vector3Int, int)> intersections)
        {
            foreach ((Vector3Int point, int collisionCode) in intersections)
            {
                materials[point].SetColor(ColorProperty, colorsMap[collisionCode]);
            }
        }
        
        public void Clear()
        {
            for (int i = 0; i < views.Count; i++)
            {
                DynamicPool.Instance.Return(views[i]);
            }
        }

        public void Move(Vector3Int position)
        {
            foreach (var view in views)
            {
                view.localPosition += (Vector3)position * _particleSize;
            }
        }
    }
}