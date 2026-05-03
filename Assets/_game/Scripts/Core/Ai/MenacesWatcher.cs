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
        public float Distance;
        public float Angle => Mathf.Acos(Dot) * Mathf.Rad2Deg;
        
        public new string ToString() => $"From: {Menace.MyUnit} ({Menace.MyUnit.SignatureId}). Angle: {Angle}";
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
            public List<IMenace> MenacesOnBoard;
            public List<MenaceRef> MenacedBy;

            public UnitData(int initialCapacity)
            {
                MenacesOnBoard = new();
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

        public IReadOnlyList<MenaceRef> GetMenaces(UnitEntity unit)
        {
            if (!_unitsWithMenaces.TryGetValue(unit, out var data))
            {
                data = new UnitData(initialMenaceCapacity);
                _unitsWithMenaces.Add(unit, data);
                return data.MenacedBy;
            }
            return data.MenacedBy;
        }
        
        public void RegisterMenace(IMenace menace)
        {
            if (!_unitsWithMenaces.TryGetValue(menace.MyUnit, out var data))
            {
                data = new UnitData(initialMenaceCapacity);
                _unitsWithMenaces.Add(menace.MyUnit, data);
                menace.MyUnit.RegisterDisposeListener(this);
            }
            data.MenacesOnBoard.Add(menace);
        }

        public void UnregisterMenace(IMenace menace)
        {
            if (_unitsWithMenaces.TryGetValue(menace.MyUnit, out var data))
            {
                data.MenacesOnBoard.Remove(menace);
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
            for (var i = 0; i < kv.Value.MenacesOnBoard.Count; i++)
            {
                var ray = kv.Value.MenacesOnBoard[i].AimingRay;

                foreach (var cell in _worldGrid.Grid.IntersectRay(ray, kv.Value.MenacesOnBoard[i].MenaceDistance))
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
                        Vector3 delta = worldEntity.Position - ray.origin;
                        float deltaLen = delta.magnitude;
                        float dot = Vector3.Dot(ray.direction, delta / deltaLen);
                        if (dot < menaceDotThreshold)
                        {
                            continue;
                        }
                        
                        if (deltaLen > kv.Value.MenacesOnBoard[i].MenaceDistance)
                        {
                            continue;
                        }

                        Debug.DrawRay(ray.origin, ray.direction * deltaLen, Color.magenta, 0.05f);
                        Debug.DrawLine(worldEntity.Position, ray.origin, Color.blueViolet, 0.05f);
                        
                        if (_unitsWithMenaces.TryGetValue(unitEntity, out var data))
                        {
                            data.MenacedBy.InsertByAscendingOrder(new MenaceRef {Menace = kv.Value.MenacesOnBoard[i], Dot = dot, Distance = deltaLen},
                                (listItem, insertItem) =>
                                {
                                    try
                                    {
                                        return (listItem.Dot * listItem.Menace.MenaceFactorValue).CompareTo(insertItem.Dot * insertItem.Menace.MenaceFactorValue);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        throw;
                                    }
                                });
                        }
                    }
                }
            }
        }
        
        public void InstallBindings(DiContainer container)
        {
            container.Bind<MenacesWatcher>().FromInstance(this);
        }
    }
}