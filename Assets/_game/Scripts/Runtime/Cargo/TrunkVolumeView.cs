using System;
using UnityEngine;

namespace Runtime.Cargo
{
    public class TrunkVolumeView : MonoBehaviour
    {
        private static readonly int Size = Shader.PropertyToID("_Size");
        [SerializeField] private MeshRenderer meshRenderer;
        
        public void SetVolume(BoundsInt volume, float particleSize)
        {
            transform.localScale = (Vector3)volume.size * particleSize;
            transform.localPosition = (Vector3)volume.position * particleSize;
            meshRenderer.material.SetVector(Size, (Vector3)volume.size * 0.5f);
        }
    }
}