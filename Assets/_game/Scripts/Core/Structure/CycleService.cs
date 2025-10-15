using System;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
using Core.Data;
using Core.Graph;
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
            public readonly IGraph graph;
            public List<IDriveInterface> Controls;
            public List<IUpdatableBlock> Updatables;
            public List<IPowerUser> PowerUsers;
            public List<IFuelUser> FuelUsers;
            public List<IForceUser> ForceUsers;

            public EntityContainer(StructureEntity entity)
            {
                Entity = entity;
                graph = entity.Structure.Graph;
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
        //private static int[] _fixedUpdateCycleCounters;
        
        public static bool isConsumptionTick = false;
        public static event Action OnEndPhysicsTick;
        public static event Action OnEndUpdateTick;
        public static LateEvent OnInitialize = new LateEvent();

        public static float DeltaTime;
        public static float Period;
        
        [Inject] private WorldGrid _worldGrid;

        protected override void Setup()
        {
            _entitiesByLod = new HashSet<EntityContainer>[GameData.Data.lodDistances.lods.Length+1];
            for (var i = 0; i < _entitiesByLod.Length; i++)
            {
                _entitiesByLod[i] = new HashSet<EntityContainer>(GameData.Data.initialStructuresCacheCapacity);
            }
            _updateCycleCounters = new int[_entitiesByLod.Length];
            //_fixedUpdateCycleCounters = new int[_entitiesByLod.Length];
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
            _entitiesByLod[lod].Remove(new EntityContainer(entity));
        }


        private void Update()
        {
            /*for (var i = 0; i < _updateCycleCounters.Length; i++)
            {
                int period = GameData.Data.lodDistances.GetLodRefreshPeriod(i);
                if (_updateCycleCounters[i]++ >= period)
                {
                    DeltaTime = Time.deltaTime * period;
                    Period = period;

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
            */
            
        }

        private void FixedUpdate()
        {
            if(!Physics.autoSimulation) return;

            for (var i = 0; i < _entitiesByLod.Length; i++)
            {
                DeltaTime = Time.deltaTime;
                /*foreach (var entityContainer in _entitiesByLod[i])
                {
                    foreach (var forceUser in entityContainer.ForceUsers)
                    {
                        IStructure str = forceUser.Structure;
                        if (str.Active && forceUser.IsActive)
                        {
                            forceUser.ApplyForce();
                        }
                    }
                }*/
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
        
        public static float Period(this float value)
        {
            return value * CycleService.Period;
        }

        public static Vector3 DeltaTime(this Vector3 value)
        {
            return value * CycleService.DeltaTime;
        }
    }
    
}
