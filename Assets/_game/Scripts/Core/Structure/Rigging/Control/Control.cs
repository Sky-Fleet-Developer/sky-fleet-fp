using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Core.Structure.Rigging.Control
{
    [System.Serializable]
    public class ControlAxe
    {
        [SerializeField] protected string keyPositive;
        [SerializeField] protected string keyNegative;
        [Space]
        public string computerInput;
        [Space]
        [SerializeField] protected float value;
        [SerializeField] protected float multiply;
        [SerializeField] protected float trim;


        //[SerializeField] private float sensitivity;
        //[SerializeField] private float gravity;
        //[SerializeField] private int frames;
        //[SerializeField] private int min;
        //[SerializeField] private int max;
        
        public float Value => value;

        public Port<float> port;
        public DeviceBase device;
        
        public void Tick()
        {
            value = Input.GetAxis(keyPositive) * multiply + trim;
            port.Value = value;
            /*int val = 0;
            if (Input.GetButton(keyPositive))
            {
                val = 1;
            }else if (Input.GetButton(keyNegative))
            {
                val = -1;
            }

            if (val != 0)
            {
                value = Mathf.MoveTowards(value,
                    Input.GetAxis(AxeControlDevices[i].Axe) * AxeControlDevices[i].Multiply,
                    Time.deltaTime * AxeControlDevices[i].Intencity);
            }
            else if (!Mathf.Approximately(value, 0))
            {
                Mathf.MoveTowards(value, 0f, Time.deltaTime * gravity);
            }*/
        }
    }
}
