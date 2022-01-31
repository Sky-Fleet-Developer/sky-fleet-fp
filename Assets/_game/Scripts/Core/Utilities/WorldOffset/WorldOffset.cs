using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.Boot_strapper;
using System.Threading.Tasks;
using Runtime.Character.Control;
using Core.SessionManager;
using System;
using Sirenix.OdinInspector;

namespace Core.Utilities
{
    public class WorldOffset : Singleton<WorldOffset>, ILoadAtStart
    {
        [ShowInInspector]
        public Vector3 Offset { get; set; }

        public static event Action<Vector3> OnWorldOffsetChange;

        [SerializeField] private float limit = 1000;
        private float limit_i;

        private Transform anchor;

        bool ILoadAtStart.enabled => true;

        protected override void Setup()
        {
            gameObject.SetActive(false);
        }

        public Task Load()
        {
            if (limit != 0)
            {
                limit_i = 1f / limit;
                Session.OnPlayerWasLoaded.Subscribe(OnPlayerWasLoaded);
            }

            return Task.CompletedTask;
        }

        private void OnPlayerWasLoaded()
        {
            gameObject.SetActive(true);
            anchor = Session.Instance.Player.transform;
        }
        
        private void Update()
        {
            Vector3 pos = anchor.position;

            Vector3 offset = Vector3.zero;            
            
            if (Mathf.Abs(pos.x) > limit)
            {
                offset += Vector3.right * pos.x;
            }
            else if(Mathf.Abs(pos.y) > limit)
            {
                offset += Vector3.up * pos.y;
            }
            else if(Mathf.Abs(pos.z) > limit)
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