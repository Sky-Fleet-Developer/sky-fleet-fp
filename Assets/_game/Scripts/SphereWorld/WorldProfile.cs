using Core.Utilities;
using Sirenix.OdinInspector;
using SphereWorld.Environment.Wind;
using UnityEngine;
using UnityEngine.Serialization;

namespace SphereWorld
{
    [CreateAssetMenu(menuName = "SphereWorld/Profile", fileName = "SphereWorldProfile")]
    public class WorldProfile : CompoundScriptableObject
    {
        public float rigidPlanetRadiusKilometers;
        public float atmosphereDepthKilometers;
        public float gravity;
        [MaxValue(100)] public float zeroHeightPercent;
    }
}