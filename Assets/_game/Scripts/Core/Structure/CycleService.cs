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
            public HashSet<IDriveInterface> Controls = new ();
            public HashSet<IUpdatableBlock> Updatables = new ();
            public HashSet<IPowerUser> PowerUsers = new ();
            public HashSet<IFuelUser> FuelUsers = new ();
            public HashSet<IForceUser> ForceUsers = new ();
            public int Lod;

            public EntityContainer(StructureEntity entity, bool needInitialize = true)
            {
                Entity = entity;
                if(!needInitialize) return;
                graph = entity.Structure.Graph;
                foreach (var structureBlock in entity.Structure.Blocks)
                {
                    AddBlock(structureBlock);
                }

                entity.Structure.OnBlockAddedEvent += AddBlock;
                entity.Structure.OnBlockRemovedEvent += RemoveBlock;
            }

            private void AddBlock(IBlock block)
            {
                if (block is IDriveInterface driveInterface)
                {
                    Controls.Add(driveInterface);
                }
                if (block is IUpdatableBlock updatableBlock)
                {
                    Updatables.Add(updatableBlock);
                }
                if (block is IPowerUser powerUser)
                {
                    PowerUsers.Add(powerUser);
                }
                if (block is IFuelUser fuelUser)
                {
                    FuelUsers.Add(fuelUser);
                }
                if (block is IForceUser forceUser)
                {
                    ForceUsers.Add(forceUser);
                }
            }

            private void RemoveBlock(IBlock block)
            {
                if (block is IDriveInterface driveInterface)
                {
                    Controls.Remove(driveInterface);
                }
                if (block is IUpdatableBlock updatableBlock)
                {
                    Updatables.Remove(updatableBlock);
                }
                if (block is IPowerUser powerUser)
                {
                    PowerUsers.Remove(powerUser);
                }
                if (block is IFuelUser fuelUser)
                {
                    FuelUsers.Remove(fuelUser);
                }
                if (block is IForceUser forceUser)
                {
                    ForceUsers.Remove(forceUser);
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

            public void Update()
            {
                Entity.Update();
                foreach (IDriveInterface t in Controls)
                {
                    IStructure str = t.Structure;
                    if (str.Active && t.GetAttachedControllersCount > 0 && t.IsActive)
                    {
                        t.ReadInput(); //TODO: continue to read input to times when axis released
                    }
                }
                foreach (IPowerUser t in PowerUsers)
                {
                    IStructure str = t.Structure;
                    if (str.Active && t.IsActive)
                    {
                        t.ConsumptionTick();
                    }
                }
                graph.UpdateGraph();
                foreach (IPowerUser t in PowerUsers)
                {
                    IStructure str = t.Structure;
                    if (str.Active && t.IsActive)
                    {
                        t.PowerTick();
                    }
                }
                foreach (IFuelUser t in FuelUsers)
                {
                    IStructure str = t.Structure;
                    if (str.Active && t.IsActive)
                    {
                        t.FuelTick();
                    }
                }
                foreach (IUpdatableBlock t in Updatables)
                {
                    IStructure str = t.Structure;
                    if (str.Active && t.IsActive)
                    {
                        t.UpdateBlock();
                    }
                }
            }

            public void FixedUpdate()
            {
                foreach (var forceUser in ForceUsers)
                {
                    IStructure str = forceUser.Structure;
                    if (str.Active && forceUser.IsActive)
                    {
                        forceUser.ApplyForce();
                    }
                }
            }
        }
        private static HashSet<EntityContainer>[] _entitiesByLod;
        private static Dictionary<StructureEntity, EntityContainer> _entities;
        private static int[] _updateCycleCounters;
        
        public static event Action OnEndPhysicsTick;
        public static event Action OnEndUpdateTick;
        public static LateEvent OnInitialize = new LateEvent();

        public static float DeltaTime;
        public static float Period;
        
        [Inject] private WorldGrid _worldGrid;

        protected override void Setup()
        {
            _entitiesByLod = new HashSet<EntityContainer>[GameData.Data.lodDistances.lods.Length+1];
            _entities = new();
            for (var i = 0; i < _entitiesByLod.Length; i++)
            {
                _entitiesByLod[i] = new HashSet<EntityContainer>(GameData.Data.initialStructuresCacheCapacity);
            }
            _updateCycleCounters = new int[_entitiesByLod.Length];
            OnInitialize.Invoke();
        }

        public static void RegisterEntity(StructureEntity entity)
        {
            if (!Application.isPlaying)
            {
                return;
            }
            entity.OnLodChangedEvent += SetEntityLod;
            var structure = entity.Structure;
            var lod = Instance._worldGrid.GetLod(entity);
            var container = new EntityContainer(entity);
            container.Lod = lod;
            _entities[entity] = container;
            _entitiesByLod[lod].Add(container);
            OnStructureInitialized?.Invoke(structure);
        }

        public static void UnregisterEntity(StructureEntity entity)
        {
            var structure = entity.Structure;
            OnStructureUnregistered?.Invoke(structure);
            var lod = Instance._worldGrid.GetLod(entity);
            _entities.Remove(entity);
            _entitiesByLod[lod].Remove(new EntityContainer(entity, false));
        }

        private static void SetEntityLod(StructureEntity entity, int lod)
        {
            var container = _entities[entity];
            _entitiesByLod[container.Lod].Remove(container);
            _entitiesByLod[lod].Add(container);
            container.Lod = lod;
        }    

        private void Update()
        {
            for (var i = 0; i < _updateCycleCounters.Length; i++)
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
                        entityContainer.Update();
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if(!Physics.autoSimulation) return;

            for (var i = 0; i < _entitiesByLod.Length; i++)
            {
                DeltaTime = Time.deltaTime;
                foreach (var entityContainer in _entitiesByLod[i])
                {
                    entityContainer.FixedUpdate();
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
