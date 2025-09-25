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
        public static event Action<Vector3> OnWorldOffsetPreChanged;

        [SerializeField] private float limit = 1000;
        [SerializeField] private bool moveY = false;
        private float limit_i;

        private Transform anchor;
        private bool _isEnabled = false;

        bool ILoadAtStart.enabled => enabled && _isEnabled;

        protected override void Setup()
        {
            _isEnabled = gameObject.activeInHierarchy;
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
            Offset = Vector3.zero;

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

            FixedUpdate();
        }
        
        private void FixedUpdate()
        {
            Vector3 pos = anchor.position;

            Vector3 offset = Vector3.zero;            
            
            if (Mathf.Abs(pos.x) > limit)
            {
                offset += Vector3.right * pos.x;
            }
            if(moveY && Mathf.Abs(pos.y) > limit)
            {
                offset += Vector3.up * pos.y;
            }
            if(Mathf.Abs(pos.z) > limit)
            {
                offset += Vector3.forward * pos.z;
            }

            if(offset != Vector3.zero) MakeOffset(-offset);
        }

        [Button]
        private void MakeOffsetDebug(Vector3 offset)
        {
            OnWorldOffsetPreChanged?.Invoke(offset);
            Offset += offset;
            Debug.Log($"WORLD_OFFSET: Target pos: {anchor.position}, current offset: {Offset}, added value: {offset}");
            OnWorldOffsetChange?.Invoke(offset);
        }
        private void MakeOffset(Vector3 offset)
        {
            for (int i = 0; i < 3; i++)
            {
                offset[i] = Mathf.Ceil(offset[i] * limit_i) * limit;
            }

            OnWorldOffsetPreChanged?.Invoke(offset);
            Offset += offset;
            Debug.Log($"WORLD_OFFSET: Target pos: {anchor.position}, current offset: {Offset}, added value: {offset}");
            OnWorldOffsetChange?.Invoke(offset);
        }

        private void OnDestroy()
        {
            OnWorldOffsetChange = null;
            OnWorldOffsetPreChanged = null;
        }
    }
}