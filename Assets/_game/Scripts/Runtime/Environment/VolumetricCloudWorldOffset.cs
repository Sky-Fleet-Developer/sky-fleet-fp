using System;
using Core.Game;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Runtime.Environment
{
    public class VolumetricCloudWorldOffset : MonoBehaviour
    {
        private VolumeProfile _volumeProfile;
        private VolumetricClouds _volumetricClouds;

        private void Awake()
        {
            _volumeProfile = GetComponent<Volume>().profile;
            _volumeProfile.TryGet(out _volumetricClouds);
            WorldOffset.OnWorldOffsetChange += OnWorldOffsetChange;
        }

        private void OnWorldOffsetChange(Vector3 offset)
        {
            _volumetricClouds.shapeOffset.value -= offset;
        }

        private void OnPreRender()
        {
            
        }
    }
}