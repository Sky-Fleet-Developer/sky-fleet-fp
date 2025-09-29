using System;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.SessionManager;
using Core.Utilities;
using Runtime.Character;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.World
{
    public class WorldOffset : MonoBehaviour, ILoadAtStart
    {
        [ShowInInspector]
        public static Vector3 Offset { get; set; }

        public static event Action<Vector3> OnWorldOffsetChange;
        public static event Action<Vector3> OnWorldOffsetPreChanged;

        [SerializeField] private float limit = 1000;
        [SerializeField] private bool moveY = false;

        private Grid _grid;
        private Transform anchor;
        private bool _isEnabled = false;

        bool ILoadAtStart.enabled => enabled && _isEnabled;

        private void Awake()
        {
            _isEnabled = gameObject.activeInHierarchy;
            gameObject.SetActive(false);
        }

        public Task Load()
        {
            Offset = Vector3.zero;
            SpawnPerson.OnPlayerWasLoaded.Subscribe(OnPlayerWasLoaded);
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

            if (limit != 0)
            {
                _grid = new Grid(Vector3.zero, limit, moveY);
                _grid.SetBorderTolerance(limit * 0.3f);
            }
            
            FixedUpdate();
        }
        
        private void FixedUpdate()
        {
            if(_grid.Update(Offset - anchor.position, out Vector3Int cell))
            {
                Vector3 offset = (Vector3)cell * _grid.Size - Offset;
                MakeOffset(offset);
            }
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