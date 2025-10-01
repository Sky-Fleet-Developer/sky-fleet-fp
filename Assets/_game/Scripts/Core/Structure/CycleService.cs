using System;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
using Core.Data;
using Core.Structure.Rigging;
using Core.Utilities;
using UnityEngine;
using Core.SessionManager;
using Core.World;
using Runtime;
using Zenject;

namespace Core.Structure
{
    public class CycleService : Singleton<CycleService>
    {
        public static event Action<IStructure> OnStructureInitialized;
        public static event Action<IStructure> OnStructureUnregistered;

        public static IEnumerable<StructureEntity> Entities()
        {
            foreach (var list in _entitiesByLod)
            {
                foreach (var container in list)
                {
                    yield return container.Entity;
                }
            }
        }
        private class EntityContainer : IEquatable<EntityContainer>
        {
            public readonly StructureEntity Entity;
            public List<IDriveInterface> Controls;
            public List<IUpdatableBlock> Updatables;
            public List<IPowerUser> PowerUsers;
            public List<IFuelUser> FuelUsers;
            public List<IForceUser> ForceUsers;

            public EntityContainer(StructureEntity entity, bool assignBlocks = true)
            {
                Entity = entity;
                if (assignBlocks)
                {
                    Controls = new(entity.Structure.GetBlocksByType<IDriveInterface>());
                    Updatables = new(entity.Structure.GetBlocksByType<IUpdatableBlock>());
                    PowerUsers = new(entity.Structure.GetBlocksByType<IPowerUser>());
                    FuelUsers = new(entity.Structure.GetBlocksByType<IFuelUser>());
                    ForceUsers = new(entity.Structure.GetBlocksByType<IForceUser>());
                }
            }

            public bool Equals(EntityContainer other)
            {
                if (other == null)
                {
                    return false;
                }
                return Entity.Equals(other.Entity);
            }

            public override int GetHashCode()
            {
                return Entity.GetHashCode();
            }
        }
        private static HashSet<EntityContainer>[] _entitiesByLod;
        private static int[] _updateCycleCounters;
        private static int[] _fixedUpdateCycleCounters;
        
        public static bool isConsumptionTick = false;
        public static event Action OnConsumptionTickEnd;
        public static event Action OnBeginConsumptionTick;
        public static event Action OnEndPhysicsTick;
        public static event Action OnEndUpdateTick;
        public static LateEvent OnInitialize = new LateEvent();

        public static float DeltaTime;
        
        [Inject] private WorldGrid _worldGrid;

        protected override void Setup()
        {
            OnConsumptionTickEnd = null;
            OnBeginConsumptionTick = null;
            _entitiesByLod = new HashSet<EntityContainer>[GameData.Data.lodDistances.lods.Length+1];
            for (var i = 0; i < _entitiesByLod.Length; i++)
            {
                _entitiesByLod[i] = new HashSet<EntityContainer>(GameData.Data.initialStructuresCacheCapacity);
            }
            _updateCycleCounters = new int[_entitiesByLod.Length];
            _fixedUpdateCycleCounters = new int[_entitiesByLod.Length];
            OnInitialize.Invoke();
        }

        public static void RegisterEntity(StructureEntity entity)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var structure = entity.Structure;
            var lod = Instance._worldGrid.GetLod(entity);
            _entitiesByLod[lod].Add(new EntityContainer(entity));
            OnStructureInitialized?.Invoke(structure);
        }

        public static void UnregisterEntity(StructureEntity entity)
        {
            var structure = entity.Structure;
            OnStructureUnregistered?.Invoke(structure);
            var lod = Instance._worldGrid.GetLod(entity);
            _entitiesByLod[lod].Remove(new EntityContainer(entity, false));
        }


        private void Update()
        {
            DeltaTime = Time.deltaTime;
            
            for (var i = 0; i < _updateCycleCounters.Length; i++)
            {
                if (_updateCycleCounters[i]++ >= GameData.Data.lodDistances.GetLodRefreshPeriod(i))
                {
                    _updateCycleCounters[i] = 0;
                    var structures = _entitiesByLod[i];
                    foreach (var entityContainer in structures)
                    {
                        foreach (IDriveInterface t in entityContainer.Controls)
                        {
                            IStructure str = t.Structure;
                            if (str.Active && t.GetAttachedControllersCount > 0 && t.IsActive)
                            {
                                t.ReadInput(); //TODO: continue to read input to times when axis released
                            }
                        }
                    }

                    isConsumptionTick = true;
                    OnBeginConsumptionTick?.Invoke();
                    
                    foreach (var entityContainer in structures)
                    {

                        foreach (IPowerUser t in entityContainer.PowerUsers)
                        {
                            IStructure str = t.Structure;
                            if (str.Active && t.IsActive)
                            {
                                t.ConsumptionTick();
                            }
                        }
                    }

                    OnConsumptionTickEnd?.Invoke();
                    isConsumptionTick = false;
                    
                    foreach (var entityContainer in structures)
                    {

                        foreach (IPowerUser t in entityContainer.PowerUsers)
                        {
                            IStructure str = t.Structure;
                            if (str.Active && t.IsActive)
                            {
                                t.PowerTick();
                            }
                        }
                    }

                    foreach (var entityContainer in structures)
                    {
                        foreach (IFuelUser t in entityContainer.FuelUsers)
                        {
                            IStructure str = t.Structure;
                            if (str.Active && t.IsActive)
                            {
                                t.FuelTick();
                            }
                        }
                    }

                    foreach (var entityContainer in structures)
                    {
                        foreach (IUpdatableBlock t in entityContainer.Updatables)
                        {
                            IStructure str = t.Structure;
                            if (str.Active && t.IsActive)
                            {
                                t.UpdateBlock();
                            }
                        }

                        OnEndUpdateTick?.Invoke();
                    }
                }
            }
            
            
        }

        private void FixedUpdate()
        {
            if(!Physics.autoSimulation) return;
            DeltaTime = Time.deltaTime;

            for (var i = 0; i < _fixedUpdateCycleCounters.Length; i++)
            {
                if (_fixedUpdateCycleCounters[i]++ >= GameData.Data.lodDistances.GetLodRefreshPeriod(i))
                {
                    _fixedUpdateCycleCounters[i] = 0;
                    
                    foreach (var entityContainer in _entitiesByLod[i])
                    {
                        foreach (var forceUser in entityContainer.ForceUsers)
                        {
                            IStructure str = forceUser.Structure;
                            if (str.Active && forceUser.IsActive)
                            {
                                forceUser.ApplyForce();
                            }
                        }
                    }
                }
            }
           
            OnEndPhysicsTick?.Invoke();
        }
    }

    public static class Extensions
    {
        public static float DeltaTime(this float value)
        {
            return value * CycleService.DeltaTime;
        }

        public static Vector3 DeltaTime(this Vector3 value)
        {
            return value * CycleService.DeltaTime;
        }
    }
    
}
