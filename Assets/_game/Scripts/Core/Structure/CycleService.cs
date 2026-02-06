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

        private class EntityContainer : IEquatable<EntityContainer>
        {
            public readonly IGraph graph;
            public readonly IStructure Structure;
            public HashSet<IDriveInterface> Controls = new ();
            public HashSet<IUpdatableBlock> Updatables = new ();
            public HashSet<IPowerUser> PowerUsers = new ();
            public HashSet<IFuelUser> FuelUsers = new ();
            public HashSet<IForceUser> ForceUsers = new ();
            public int Lod;


            public EntityContainer(IStructure structure, bool needInitialize = true)
            {
                Structure = structure;
                if(!needInitialize) return;
                graph = structure.Graph;
                foreach (var structureBlock in structure.Blocks)
                {
                    AddBlock(structureBlock);
                }

                structure.OnBlockAddedEvent += AddBlock;
                structure.OnBlockRemovedEvent += RemoveBlock;
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

                if (Structure == null)
                {
                    return other.Structure == null;
                }
                return Structure.Equals(other.Structure);
            }

            public override int GetHashCode()
            {
                return Structure.GetHashCode();
            }

            public void Update()
            {
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

        public static IEnumerable<IStructure> Structures()
        {
            foreach (var list in _entitiesByLod)
            {
                foreach (var container in list)
                {
                    yield return container.Structure;
                }
            }
        }

        private static HashSet<EntityContainer>[] _entitiesByLod;
        private static Dictionary<IStructure, EntityContainer> _structures;
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
            _structures = new();
            for (var i = 0; i < _entitiesByLod.Length; i++)
            {
                _entitiesByLod[i] = new HashSet<EntityContainer>(GameData.Data.initialStructuresCacheCapacity);
            }
            _updateCycleCounters = new int[_entitiesByLod.Length];
            OnInitialize.Invoke();
        }

        public static void RegisterStructure(IStructure structure)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var lod = 0;//Instance._worldGrid.GetLod(structure);
            var container = new EntityContainer(structure);
            container.Lod = lod;
            _structures[structure] = container;
            _entitiesByLod[lod].Add(container);
            OnStructureInitialized?.Invoke(structure);
        }

        public static void UnregisterStructure(IStructure structure)
        {
            OnStructureUnregistered?.Invoke(structure);
            var lod = 0;//Instance._worldGrid.GetLod(structure);
            _structures.Remove(structure);
            _entitiesByLod[lod].Remove(new EntityContainer(structure, false));
        }

        public static void SetLodToStructure(IStructure entity, int lod)
        {
            var container = _structures[entity];
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
