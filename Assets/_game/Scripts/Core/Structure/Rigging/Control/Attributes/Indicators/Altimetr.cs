using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes.Indicators
{
    public class Altimetr : ArrowIndicator
    {
        [SerializeField] private float maxHeightKilometers;
        [SerializeField] private float maxHeightMeters;

        [SerializeField] private float maxAngleKilometers;
        [SerializeField] private float maxAngleMeters;

        private float startSideKilometers;
        private float startSideMeters;

        private void Start()
        {
            startSideKilometers = arrows[0].localEulerAngles.y;
            startSideMeters = arrows[1].localEulerAngles.y;
        }

        public override void UpdateDevice()
        {
            float heightK = wire.value / 1000;
            float heightM = wire.value % 1000;

            if(heightK > maxHeightKilometers)
            {
                heightM = 0;
            }

            heightK = Mathf.Min(maxHeightKilometers, heightK);
            heightM = Mathf.Min(maxHeightMeters, heightM);

            float angleK = ConvertRange(new Vector2(0, maxHeightKilometers), new Vector2(0, maxAngleKilometers), heightK);
            float angleM = ConvertRange(new Vector2(0, maxHeightMeters), new Vector2(0, maxAngleMeters), heightM);
            arrows[0].localRotation = GetRotateArrow(startSideKilometers, angleK);
            arrows[1].localRotation = GetRotateArrow(startSideMeters, angleM);
        }
    }
}