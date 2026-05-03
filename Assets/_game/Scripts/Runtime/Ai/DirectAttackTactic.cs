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
        private float _noiseOffset = Random.Range(0f, 1f);

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
            float d = ControlledEntity.Unit.Sensor.Distance(Target, _characteristic.turn180Time * 0.2f);
            if (d > _characteristic.maxAttackRange)
            {
                SetState(State.Approaching);
                return;
            }
            var noise = Mathf.PerlinNoise1D(_noiseOffset + Time.time * 2) - 0.5f;
            float rangeNoised = d * (1 + noise * 1.2f);

            if (rangeNoised > _characteristic.minAttackRange)
            {
                if (ControlledEntity.Unit.Sensor.Dot(Target) > 0 || rangeNoised > _characteristic.minAttackRange * 2)
                {
                    SetState(State.Aiming);
                    return;
                }
            }

            //Debug.Log($"Retreating ({ControlledEntity.Id}). d = {d}, noise = {noise}");
            SetState(State.Retreating);
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
                case State.Aiming:
                case State.Shooting:
                    ControlledEntity.Unit.SetManeuvers(new RotateTowards(Target), new Aiming(Target, true));
                    break;
                case State.Retreating:
                    if (Target.Position.y > ControlledEntity.Position.y)
                    {
                        ControlledEntity.Unit.SetManeuvers(new DownAway());
                    }
                    else
                    {
                        ControlledEntity.Unit.SetManeuvers(new UpAway(ControlledEntity.Position.y + 150,
                            ControlledEntity.GetTechCharacteristic().cruiseLiftAngle, 15));
                    }

                    break;
            }

            //Debug.Log($"{state}: ({ControlledEntity.Id})");
            _state = state;
        }

        public override bool CanChangeTo(Type newTacticType, UnitEntity entity)
        {
            if (newTacticType == typeof(MenaceReactionTactic))
            {
                MenaceRef menaceToSelf = entity.Unit.Sensor.Menaces[0];
                var mainMenaceOfSelfMenace = menaceToSelf.Menace.MyUnit.Unit.Sensor.Menaces;

                // Search for self menace to other unit. Do they are menace to each other?
                MenaceRef? selfMenaceToOther = null;
                for (var i = 0; i < mainMenaceOfSelfMenace.Count; i++)
                {
                    if (mainMenaceOfSelfMenace[i].Menace.MyUnit == entity)
                    {
                        selfMenaceToOther = mainMenaceOfSelfMenace[i]; break;
                    }
                }

                if (!selfMenaceToOther.HasValue) // I'm not menace to other unit.
                {
                    return true; // I have to react.
                }

                // I'm menace to other unit.
                // If my weapon is strong enough, I have to shoot him.
                if (menaceToSelf.Menace.MenaceFactorValue > selfMenaceToOther.Value.Menace.MenaceFactorValue * 0.9f)
                {
                    return false; // Do not react.
                }

                // My weapon is weaker than other unit, but his weapon is not aimed to me yet. I have to shoot him.
                if (menaceToSelf.Dot > selfMenaceToOther.Value.Dot)
                {
                    return false; // Do not react.
                }
            }

            return true;
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

        public string GetDescription()
        {
            return $"State: {_state}. Target: {Target}";
        }
    }
}