using System;
using Core.Character;
using Runtime.Character;
using UnityEngine;

namespace SphereWorld.Character
{
    [RequireComponent(typeof(WorldEntity), typeof(CharacterMotor))]
    public class CharacterEntityAdaptor : MonoBehaviour
    {
        private WorldEntity _entity;
        private CharacterMotor _motor;
        private void Awake()
        {
            _entity = GetComponent<WorldEntity>();
            _motor = GetComponent<CharacterMotor>();
            _entity.OnEntityBecameVisible += OnEntityBecameVisible;
        }

        private void OnEntityBecameVisible()
        {
            transform.position = _entity.GetOffset();   
            _motor.ResetPlatform();
        }
    }
}