using Core.Graph.Wires;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Rigging.Control.Attributes
{
    public abstract class SingleDevice : DeviceBase<Port<float>>
    {
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue;
        [SerializeField] private float sensitivity;
        [SerializeField] private bool setDefaultOnExitControl;
        [SerializeField][ShowIf("setDefaultOnExitControl")] private float defaultValue;
        public override void MoveValueInteractive(float val)
        {
            Port.SetValue(Mathf.Clamp(Port.Value + val * sensitivity, minValue, maxValue));
        }

        public override void ExitControl()
        {
            if (setDefaultOnExitControl)
            {
                Port.SetValue(defaultValue);
            }
        }
    }
}