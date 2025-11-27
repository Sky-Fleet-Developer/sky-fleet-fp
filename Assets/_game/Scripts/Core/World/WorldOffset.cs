using System;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.SessionManager;
using Core.Utilities;
using Runtime.Character;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Core.World
{

    public class WorldOffset : MonoBehaviour, ILoadAtStart, IMyInstaller, WorldOffset.IWorldOffsetHandler
    {
        [ShowInInspector]
        public static Vector3 Offset { get; set; }
        public static event Action<Vector3> OnWorldOffsetChange;
        public static event Action<Vector3> OnWorldOffsetPreChanged;

        [SerializeField] private float limit = 1000;
        [SerializeField] private bool moveY = false;
        [Inject(Optional = true)] private IWorldOffsetHandler _worldOffsetHandler;
        [Inject(Optional = true)] private Session _session;
        private Grid _grid;
        private Transform anchor;
        private bool _isEnabled = false;
        private bool _isManualControl = false;
        bool ILoadAtStart.enabled => enabled && _isEnabled;

        private void Start()
        {
            _isEnabled = gameObject.activeInHierarchy;
            gameObject.SetActive(false);
            Offset = Vector3.zero;
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
            
            if (_session.Player)
            {
                anchor = _session.Player.transform;
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
            if(_isManualControl) return;
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
            Debug.Log($"WORLD_OFFSET: current offset: {Offset}, added value: {offset}");
            OnWorldOffsetChange?.Invoke(offset);
        }
        private void MakeOffset(Vector3 offset)
        {
            OnWorldOffsetPreChanged?.Invoke(offset);
            Offset += offset;
            Debug.Log($"WORLD_OFFSET: current offset: {Offset}, added value: {offset}");
            OnWorldOffsetChange?.Invoke(offset);
        }

        private void OnDestroy()
        {
            OnWorldOffsetChange = null;
            OnWorldOffsetPreChanged = null;
        }
        
        public interface IWorldOffsetHandler
        {
            void SetOffset(Vector3 offset);
            void TakeControl();
            void ReleaseControl();
        }

        void IWorldOffsetHandler.SetOffset(Vector3 offset)
        {
            var delta = offset - Offset;
            if(delta != Vector3.zero) MakeOffset(delta);
        }

        void IWorldOffsetHandler.TakeControl()
        {
            _isManualControl = true;
        }

        void IWorldOffsetHandler.ReleaseControl()
        {
            _isManualControl = false;
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<IWorldOffsetHandler>().FromInstance(this);
        }
    }
}