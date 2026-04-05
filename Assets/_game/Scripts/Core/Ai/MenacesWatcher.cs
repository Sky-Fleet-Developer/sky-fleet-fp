using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Character.Interaction;
using Core.Misc;
using Core.Structure;
using Core.World;
using UnityEngine;
using Zenject;
using ITickable = Core.Misc.ITickable;

namespace Core.Ai
{
    public struct MenaceRef
    {
        public IMenace Menace;
        public float Dot;
    }
    
    public class MenacesWatcher : MonoBehaviour, IMyInstaller, ITickable, IWorldEntityDisposeListener
    {
        [SerializeField] private int initialMenaceCapacity = 100;
        [SerializeField] private float menaceDotThreshold = 0.9f;
        //[SerializeField] private float menaceRange;
        [Inject] private TickService _tickService;
        [Inject] private WorldGrid _worldGrid;
        [Inject] private TableRelations _tableRelations;
        private Dictionary<UnitEntity, UnitData> _unitsWithMenaces = new();
        public int TickRate => 10;

        private struct UnitData
        {
            public List<IMenace> Menaces;
            public List<MenaceRef> MenacedBy;

            public UnitData(int initialCapacity)
            {
                Menaces = new();
                MenacedBy = new(initialCapacity);
            }
        }
        
        private void Start()
        {
            _tickService.Add(this);
        }

        private void OnDestroy()
        {
            _tickService.Remove(this);
        }

        public IReadOnlyList<MenaceRef> GetMenaces(UnitEntity unit) => _unitsWithMenaces[unit].MenacedBy;
        
        public void RegisterMenace(IMenace menace)
        {
            if (!_unitsWithMenaces.TryGetValue(menace.MyUnit, out var data))
            {
                data = new UnitData(initialMenaceCapacity);
                _unitsWithMenaces.Add(menace.MyUnit, data);
                menace.MyUnit.RegisterDisposeListener(this);
            }
            data.Menaces.Add(menace);
        }

        public void UnregisterMenace(IMenace menace)
        {
            if (_unitsWithMenaces.TryGetValue(menace.MyUnit, out var data))
            {
                data.Menaces.Remove(menace);
            }
        }
        
        public void OnEntityDisposed(IWorldEntity entity)
        {
            _unitsWithMenaces.Remove((UnitEntity)entity);
        }
        
        public void Tick()
        {
            Parallel.ForEach(_unitsWithMenaces, ClearMenaces);
            Parallel.ForEach(_unitsWithMenaces, CheckMenaces);
        }

        private void ClearMenaces(KeyValuePair<UnitEntity, UnitData> kv)
        {
            kv.Value.MenacedBy.Clear();
        }

        private void CheckMenaces(KeyValuePair<UnitEntity, UnitData> kv)
        {
            var unitSign = kv.Key.SignatureId;
            for (var i = 0; i < kv.Value.Menaces.Count; i++)
            {
                var ray = kv.Value.Menaces[i].AimingRay;

                foreach (var cell in _worldGrid.Grid.IntersectRay(ray, Mathf.Sqrt(kv.Value.Menaces[i].MenaceDistanceSqr)))
                {
                    foreach (var worldEntity in _worldGrid.EnumerateCell(cell))
                    {
                        if (worldEntity is not UnitEntity unitEntity)
                        {
                            continue;
                        }

                        var relation = _tableRelations.GetRelation(unitEntity.SignatureId, unitSign);

                        if (relation >= RelationType.Neutral)
                        {
                            continue;
                        }
                        
                        float dot = Vector3.Dot(ray.direction, worldEntity.Position - ray.origin);
                        if (dot < menaceDotThreshold)
                        {
                            continue;
                        }
                        
                        float distanceSqr = (worldEntity.Position - ray.origin).sqrMagnitude;
                        if (distanceSqr > kv.Value.Menaces[i].MenaceDistanceSqr)
                        {
                            continue;
                        }
                        
                        kv.Value.MenacedBy.Add(new MenaceRef {Menace = kv.Value.Menaces[i], Dot = dot});
                    }
                }
            }
        }
        
        public void InstallBindings(DiContainer container)
        {
            container.Bind<MenacesWatcher>().AsSingle();
        }
    }
}