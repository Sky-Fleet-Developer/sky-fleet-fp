using System;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [Serializable]
    public class HueShift : IColorFilter
    {
        [SerializeField] private float hueAdd;
        [SerializeField] private float saturationAdd;
        [SerializeField] private float valueAdd;

        public Color Evaluate(Color reference)
        {
            Color.RGBToHSV(reference, out float h, out float s, out float v);
            var result = Color.HSVToRGB((h + hueAdd) % 1f, Mathf.Clamp(s + saturationAdd, 0f, 1f),
                Mathf.Clamp(v + valueAdd, 0f, 1f));
            /*if (hueAdd == 0 && saturationAdd == 0 && valueAdd == 0 && result != reference)
            {
                
            }*/
            return result;
        }
    }
}
