using System;
using System.Collections.Generic;
using Core.Ai;
using Core.Misc;
using Core.World;
using Runtime.Ai.Maneuvers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.Ai
{
    public class DirectAttackTactic : SingleUnitTacticBase
    {
        public enum State
        {
            Idle,
            Approaching,
            Aiming,
            Shooting,
            Retreating
        }

        public DirectAttackTactic(TickService tickService) : base(tickService)
        {
        }

        public ISignatureData Target { get; set; }
        private State _state;
        private UnitTechCharacteristic _characteristic;
        private float _attackRangeMul = Random.Range(0.7f, 1.3f);

        public override void UnitEnterTactic(UnitEntity entity)
        {
            base.UnitEnterTactic(entity);
            _characteristic = ControlledEntity.GetTechCharacteristic();
            if (Target == null)
            {
                SetState(State.Idle);
                return;
            }
            
            DefineCombatState();
        }

        private void DefineCombatState()
        {
            // TODO: make reaction to attack, retreat faster when enemy attacks me
            float d = ControlledEntity.Unit.Sensor.Distance(Target, _characteristic.turn180Time * 0.2f) * _attackRangeMul;
            if (d > _characteristic.maxAttackRange)
            {
                SetState(State.Approaching);
            }
            else if (d > _characteristic.minAttackRange)
            {
                SetState(State.Aiming);
            }
            else
            {
                SetState(State.Retreating);
            }
        }

        private void SetState(State state)
        {
            if (state == _state)
            {
                return;
            }
            switch (state)
            {
                case State.Idle:
                    ControlledEntity.Unit.SetManeuvers(new FlyStraight());
                    break;
                case State.Approaching:
                    ControlledEntity.Unit.SetManeuvers(new Follow(Target, Vector3.zero));
                    break;
                case State.Aiming: case State.Shooting:
                    ControlledEntity.Unit.SetManeuvers(new Aiming(Target));
                    break;
                case State.Retreating:
                    if (Target.Position.y > ControlledEntity.Position.y)
                    {
                        ControlledEntity.Unit.SetManeuvers(new DownAway());
                    }
                    else
                    {
                        ControlledEntity.Unit.SetManeuvers(new UpAway(ControlledEntity.Position.y + 150, ControlledEntity.GetTechCharacteristic().cruiseLiftAngle));
                    }
                    break;
            }
            Debug.Log($"{state}");
            _state = state;
        }

        public override void Tick()
        {
            if (ControlledEntity == null)
            {
                Dispose();
                return;
            }
            if (_state == State.Retreating)
            {
                if (!ControlledEntity.Unit.IsManeuversComplete)
                {
                    return;
                }
            }
            DefineCombatState();
        }
    }
}