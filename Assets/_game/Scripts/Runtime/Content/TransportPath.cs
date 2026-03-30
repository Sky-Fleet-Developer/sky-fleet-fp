using System;
using System.Collections.Generic;
using AYellowpaper;
using Core.Ai;
using Core.Data;
using Core.Items;
using Core.World;
using Runtime.Misc;
using UnityEngine;
using Zenject;

namespace Runtime.Content
{
    public interface ITransportSpawnBehaviour
    {
        public UnitEntity Spawn(EntityObjectInstaller source, Vector3 position, Quaternion rotation, string overrideSignature = null);
    }

    [RequireComponent(typeof(SpsViewRange))]
    public class TransportPath : MonoBehaviour
    {
        [SerializeField] private float viewRange;
        [SerializeField] private EntityObjectInstaller entityToSpawn;
        [SerializeField] private SignatureId signatureOverride;
        [SerializeField, SerializeReference] private ITransportSpawnBehaviour spawnBehaviour;
        [SerializeField] private InterfaceReference<IAiPathStrategy> attachedStrategy;
        [Inject(Id = "Player")] private IDynamicPositionProvider _playerTracker;
        [Inject] private WorldSpace _worldSpace;
        private SpsViewRange _viewRangeSpline;
        private float _viewRangeSqr;
        
        [Inject]
        private void Inject(DiContainer container)
        {
            container.Inject(spawnBehaviour);
        }
        
        private void Awake()
        {
            _viewRangeSpline = GetComponent<SpsViewRange>();
            _viewRangeSpline.OnPointBecameVisible += OnPointBecameVisible;
            _viewRangeSpline.OnPointBecameInvisible += OnPointBecameInvisible;
            _viewRangeSqr = viewRange * viewRange;
            _viewRangeSpline.SetViewRange(viewRange);
        }

        private void OnDestroy()
        {
            _viewRangeSpline.OnPointBecameVisible -= OnPointBecameVisible;
            _viewRangeSpline.OnPointBecameInvisible -= OnPointBecameInvisible;
        }

        private void OnPointBecameVisible(SpsPoint point)
        {
            _viewRangeSpline.Spline.EvaluatePoint(point, out var position, out var rotation, out bool isReverse, out int lap);
            var entity = spawnBehaviour.Spawn(entityToSpawn, position, rotation, signatureOverride);
            attachedStrategy.Value.Link(entity.Id, point.Index);
            attachedStrategy.Value.AddControllableUnit(entity);
        }

        private void Update()
        {
            var entities = attachedStrategy.Value.GetControllableUnits();
            for (var i = 0; i < entities.Count; i++)
            {
                if ((entities[i].Position - _playerTracker.SpacePosition).sqrMagnitude > _viewRangeSqr)
                {
                    var e = entities[i];
                    attachedStrategy.Value.RemoveControllableUnit(e);
                    _worldSpace.RemoveEntity(e);
                    e.Dispose();
                    i--;
                }
            }
        }

        private void OnPointBecameInvisible(SpsPoint point)
        {
            //if (_entities.TryGetValue(point, out var entity))
            {
                //_worldSpace.RemoveEntity(entity);
                //entity.Dispose();
            }
            //else
            {
                //Debug.LogError($"Entity at point [{point.Index}] was not found to remove");
            }
        }
    }
}