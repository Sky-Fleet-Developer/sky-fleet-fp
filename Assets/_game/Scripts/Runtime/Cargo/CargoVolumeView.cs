using System.Collections.Generic;
using Core.Utilities;
using UnityEngine;

namespace Runtime.Cargo
{
    public class CargoVolumeView : MonoBehaviour
    {
        [SerializeField] private Transform cube;
        private readonly List<Transform> views = new();
        private float _particleSize;

        private void Awake()
        {
            DynamicPool.Instance.Return(cube);
        }

        public void SetVolume(IReadOnlyList<Vector3Int> volume, float particleSize)
        {
            _particleSize = particleSize;
            for (var i = 0; i < volume.Count; i++)
            {
                if (views.Count == i)
                {
                    views.Add(DynamicPool.Instance.Get(cube, transform));
                }

                views[i].localPosition = (Vector3)volume[i] * particleSize;
                views[i].localScale = Vector3.one * particleSize;
            }

            for (int i = volume.Count; i < views.Count; i++)
            {
                DynamicPool.Instance.Return(views[i]);
            }
            views.Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < views.Count; i++)
            {
                DynamicPool.Instance.Return(views[i]);
            }
        }

        public void Move(Vector3Int offset)
        {
            foreach (var view in views)
            {
                view.localPosition += (Vector3)offset * _particleSize;
            }
        }
    }
}