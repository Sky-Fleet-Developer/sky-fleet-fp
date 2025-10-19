using System;
using UnityEngine;

namespace SphereWorld.Environment.Wind
{
    [CreateAssetMenu(menuName = "SF/SphereWorld/Atmosphere", fileName = "SphereWorldAtmosphere")]
    [System.Serializable]
    public class AtmosphereProfile : ScriptableObject
    {
        public float zeroHeightPressure;
        public float zeroHeightTemperature;
        
        private const float MolarAirMass = 0.0289644f; // kg/mol
        private const float MolarGasConstant = 8.3144598f; // j/mol
        private const float PressureCoefficient = MolarAirMass / MolarGasConstant;
            
        public float EvaluatePressurePercent(float gravity, float height)
        {
            return zeroHeightPressure * Mathf.Exp(-gravity * PressureCoefficient * height / zeroHeightTemperature);
        }
        public double EvaluatePressurePercent(double gravity, double height)
        {
            return zeroHeightPressure * Math.Exp(-gravity * PressureCoefficient * height / zeroHeightTemperature);
        }
    }
}