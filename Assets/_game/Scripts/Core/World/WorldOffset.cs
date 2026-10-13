using System;
using System.Threading.Tasks;
using Core.Explorer;
using Core.SessionManager;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Core.World
{

    public class WorldOffset : MonoBehaviour, IMyInstaller, WorldOffset.IWorldOffsetHandler
    {
        [ShowInInspector]
        public static Vector3 Offset
        {
            get; 
            set; 
        }
        public static Vector3 SpaceToWorld(Vector3 point) => point - Offset;
        public static Vector3 WorldToSpace(Vector3 point) => point + Offset;
        public static event Action<Vector3> OnWorldOffsetChange;
        public static event Action<Vector3> OnWorldOffsetPreChanged;

        [SerializeField] private float limit = 1000;
        [SerializeField] private bool moveY = false;
        [Inject(Optional = true)] private IWorldOffsetHandler _worldOffsetHandler;
        [Inject(Optional = true)] private Session _session;
        private Grid _grid;
        private Transform _anchor;
        private bool _isManualControl = false;

        private void Start()
        {
            gameObject.SetActive(false);
            Bootstrapper.OnLoadComplete.Subscribe(OnInitComplete);
            Offset = Vector3.zero;
        }

        private void OnInitComplete()
        {
            gameObject.SetActive(true);
            
            if (_session.Player)
            {
                _anchor = _session.Player.transform;
            }
            else
            {
                _anchor = transform;
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
            if(_grid.Update(Offset - _anchor.position, out Vector3Int cell))
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
            //Debug.Log($"WORLD_OFFSET: current offset: {Offset}, added value: {offset}");
            OnWorldOffsetChange?.Invoke(offset);
        }
        private void MakeOffset(Vector3 offset)
        {
            OnWorldOffsetPreChanged?.Invoke(offset);
            Offset += offset;
            //Debug.Log($"WORLD_OFFSET: current offset: {Offset}, added value: {offset}");
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