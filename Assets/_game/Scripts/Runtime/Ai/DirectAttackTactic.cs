using System;
using System.Collections.Generic;
using Core.Ai;
using Core.Misc;
using Core.World;
using Runtime.Ai.Maneuvers;
using UnityEngine;

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
            float d = ControlledEntity.Unit.Sensor.Distance(Target);
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
                    ControlledEntity.Unit.SetManeuvers(new DownAway());
                    break;
            }
            _state = state;
        }

        public override void Tick()
        {
            DefineCombatState();
        }
    }
}