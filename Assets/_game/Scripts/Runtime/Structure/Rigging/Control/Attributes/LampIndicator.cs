using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class LampIndicator : DeviceBase<bool>
    {
        [SerializeField] private GameObject activeIndicatorObj;
        [SerializeField] private GameObject noactiveIndicatorObj;

        bool oldValue;

        public override void Init(IStructure structure, IBlock block)
        {
            base.Init(structure, block);
            oldValue = false;
            activeIndicatorObj.SetActive(false);
            noactiveIndicatorObj.SetActive(true);
        }

        public override void UpdateDevice()
        {
            if (oldValue != port.Value)
            {
                oldValue = port.Value;
                if (oldValue)
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