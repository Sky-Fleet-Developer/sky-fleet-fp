using System;
using System.Collections.Generic;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using static Core.Structure.StructureUpdateModule;

namespace Core.Structure.Rigging.Control
{
    [Serializable]
    public class ControlTrackball : IVisibleControlElement
    {
        public enum TypeTrackballLimit
        {
            Rect = 0,
            Round = 1,
        }

        [HideInInspector]
        public Port PortAbstact { get => Port; }

        [ShowInInspector]
        public Port<Vector2> Port;

        [ShowInInspector]
        public DeviceBase Device { get => _device; set => _device = value; }


        [SerializeField] protected AxeInput axeX;
        [SerializeField] protected AxeInput axeY;
        [SerializeField] protected TypeTrackballLimit typeLimit;
        [SerializeField, Range(0.1f, 4f)] protected float multiply = 1;

        [SerializeField, HideInInspector]
        private DeviceBase _device;

        private Vector2 currentPos = Vector3.zero;

        private Vector2 GetPos()
        {

            Vector2 delta = new Vector2(axeX.GetValue(), -axeY.GetValue());
            delta.x = delta.x * multiply;
            delta.y = delta.y * multiply;
            return currentPos + delta;
        }

        public void Tick()
        {
            Vector2 pos = GetPos();
            if(typeLimit == TypeTrackballLimit.Rect)
            {
                pos.x = Mathf.Clamp(pos.x, -1, 1);
                pos.y = Mathf.Clamp(pos.y, -1, 1);
            }
            else
            {
                pos = Vector2.ClampMagnitude(pos, 1);
            }
            currentPos = pos;
            Port.Value = currentPos;

        }
    }
}