using System;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Runtime.Physic;
using Runtime.Structure.Rigging.Power;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class Winch : PowerUserBlock,IForceUser
    {
        private Port<float> input = new Port<float>(PortType.Signal);
        private ActionPort detach = new ();
        [SerializeField] private Rope rope;
        [SerializeField] private float minLength;
        [SerializeField] private float maxLength;
        [SerializeField] private float winchSpeed;
        [SerializeField] private float maxTension;
        [SerializeField] private float maxConsumption;
        [SerializeField] private AnimationCurve consumptionCurve;
        private float _driveMotor;
        private float _currentLength;
        private float _wantedLength;

        private void Awake()
        {
            rope.OnInitialize.Subscribe(() =>
            {
                _currentLength = _wantedLength = rope.GetDistance();
            });
            detach.AddRegisterAction(Detach);
        }

        private void Detach()
        {
            if (rope.IsConnected)
            {
                rope.Detach();
            }
        }

        public override float Consumption
        {
            get
            {
                _driveMotor = input.GetValue();
                return consumptionCurve.Evaluate(Mathf.Abs(_driveMotor)) * maxConsumption;
            }
        }

        public void ApplyForce()
        {
            if (!rope.OnInitialize.alredyInvoked)
            {
                return;
            }
            _currentLength = rope.GetDistance();
            _wantedLength += _driveMotor * winchSpeed.DeltaTime();
            if (_currentLength > _wantedLength)
            {
                _wantedLength = Mathf.MoveTowards(_currentLength, _wantedLength, maxTension);
            }

            rope.Length = Mathf.Clamp(_wantedLength, minLength, maxLength);
        }
    }
}