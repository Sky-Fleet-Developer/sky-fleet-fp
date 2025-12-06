using System;
using Core.World;
using UnityEngine;
using Zenject;

namespace Runtime.Environment.MacroShading
{
    public class MacroShadingProcessor : MonoBehaviour
    {
        [SerializeField] private float updateRate = 1f;
        [SerializeField] private Camera renderCamera;
        [SerializeField] private Transform sun;
        
        [Inject(Id = "Player")]
        private IDynamicPositionProvider _player;
        private float _lastUpdate;
        
        private void Update()
        {
            if (Time.time - _lastUpdate > updateRate)
            {
                _lastUpdate = Time.time;
                renderCamera.transform.rotation = sun.rotation;
                renderCamera.transform.position = _player.SpacePosition - renderCamera.transform.forward * renderCamera.farClipPlane;
                renderCamera.aspect = 1f;
                renderCamera.Render();
            }
        }
    }
}