using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes.Indicators
{
    public class IndicatorDiod : IndicatorBase<bool>
    {
        [SerializeField] private GameObject activeIndicatorObj;
        [SerializeField] private GameObject noactiveIndicatorObj;

        bool oldValue;

        public override void Init(IStructure structure, IBlock block, string port)
        {
            base.Init(structure, block, port);
            oldValue = false;
            activeIndicatorObj.SetActive(false);
            noactiveIndicatorObj.SetActive(true);
        }

        public override void UpdateDevice()
        {
            if(oldValue != wire.value)
            {
                oldValue = wire.value;
                if(oldValue)
                {
                    activeIndicatorObj.SetActive(true);
                    noactiveIndicatorObj.SetActive(false);
                }
                else
                {
                    activeIndicatorObj.SetActive(false);
                    noactiveIndicatorObj.SetActive(true);
                }
            }
        }
    }
}