using System;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.SessionManager;
using Core.Utilities;
using Runtime.Character;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Game
{
    public class WorldOffset : Singleton<WorldOffset>, ILoadAtStart
    {
        [ShowInInspector]
        public static Vector3 Offset { get; set; }

        public static event Action<Vector3> OnWorldOffsetChange;

        [SerializeField] private float limit = 1000;
        private float limit_i;

        private Transform anchor;

        bool ILoadAtStart.enabled => true;

        protected override void Setup()
        {
            gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            if (limit != 0)
            {
                limit_i = 1f / limit;
            }
        }

        public Task Load()
        {
            if (limit != 0)
            {
                limit_i = 1f / limit;
                SpawnPerson.OnPlayerWasLoaded.Subscribe(OnPlayerWasLoaded);
            }

            return Task.CompletedTask;
        }

        private void OnPlayerWasLoaded()
        {
            gameObject.SetActive(true);
            if (Session.hasInstance)
            {
                anchor = Session.Instance.Player.transform;
            }
            else
            {
                anchor = transform;
            }

            MakeOffset(-Offset);
        }
        
        private void Update()
        {
            Vector3 pos = anchor.position;

            Vector3 offset = Vector3.zero;            
            
            if (Mathf.Abs(pos.x) > limit)
            {
                offset += Vector3.right * pos.x;
            }
            if(Mathf.Abs(pos.y) > limit)
            {
                offset += Vector3.up * pos.y;
            }
            if(Mathf.Abs(pos.z) > limit)
            {
                offset += Vector3.forward * pos.z;
            }

            if(offset != Vector3.zero) MakeOffset(-offset);
        }

        private void MakeOffset(Vector3 offset)
        {
            for (int i = 0; i < 3; i++)
            {
                offset[i] = Mathf.Ceil(offset[i] * limit_i) * limit;
            }
            
            Offset += offset;
            OnWorldOffsetChange?.Invoke(offset);
        }
    }
}