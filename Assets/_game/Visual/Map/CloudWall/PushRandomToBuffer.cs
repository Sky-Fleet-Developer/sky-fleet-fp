/*using System;
using Sirenix.OdinInspector;
using UnityEngine;

    public class PushRandomToBuffer : MonoBehaviour
    {
        private static readonly int Rnd = Shader.PropertyToID("random_numbers");
        [SerializeField] private Vector2 range;
        [SerializeField] public MeshRenderer meshRenderer;
        private GraphicsBuffer _buffer;

        private void Awake()
        {
            Setup();
        }

        [Button]
        private void Setup()
        {
            if (_buffer != null)
            {
                _buffer.Dispose();
            }
            _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 128, sizeof(float));
            float[] data = new float[128];
            for(int i = 0; i < 128; i++)
            {
                data[i] = UnityEngine.Random.Range(range.x, range.y);
            }
            
            _buffer.SetData(data);
            if (Application.isPlaying)
            {
                meshRenderer.material.SetBuffer(Rnd, _buffer);
            }
            else
            {
                meshRenderer.sharedMaterial.SetBuffer(Rnd, _buffer);
            }
            //Shader.SetGlobalBuffer(Rnd, _buffer);
        }

        private void OnDestroy()
        {
            _buffer.Dispose();
        }
    }
*/