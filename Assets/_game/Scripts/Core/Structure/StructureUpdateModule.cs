using System;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
using Core.Structure.Rigging;
using Core.Utilities;
using UnityEngine;
using Core.SessionManager;
using Runtime;

namespace Core.Structure
{
    public class StructureUpdateModule : Singleton<StructureUpdateModule>
    {
        public static event Action<IStructure> OnStructureInitialized;
        public static event Action<IStructure> OnStructureDestroy;
        
        public static List<IStructure> Structures = new List<IStructure>();
        public static List<ICharacterInterface> Controls = new List<ICharacterInterface>();
        public static List<IUpdatableBlock> Updatables = new List<IUpdatableBlock>();
        public static List<IPowerUser> PowerUsers = new List<IPowerUser>();
        public static List<IFuelUser> FuelUsers = new List<IFuelUser>();
        public static List<IForceUser> ForceUsers = new List<IForceUser>();

        public static bool isConsumptionTick = false;
        public static event Action OnConsumptionTickEnd;
        public static event Action OnBeginConsumptionTick;
        public static event Action OnEndPhysicsTick;
        public static event Action OnEndUpdateTick;

        public static float DeltaTime;

        protected override void Setup()
        {
            OnConsumptionTickEnd = null;
            OnBeginConsumptionTick = null;
        }

        public static void RegisterStructure(IStructure structure)
        {
            if (!Application.isPlaying)
            {
                return;
            }
            Structures.Add(structure);
            Controls.AddRange(structure.GetBlocksByType<ICharacterInterface>());
            Updatables.AddRange(structure.GetBlocksByType<IUpdatableBlock>());
            PowerUsers.AddRange(structure.GetBlocksByType<IPowerUser>());
            FuelUsers.AddRange(structure.GetBlocksByType<IFuelUser>());
            ForceUsers.AddRange(structure.GetBlocksByType<IForceUser>());
            OnStructureInitialized?.Invoke(structure);
        }

        public static void DestroyStructure(IStructure structure)
        {
            OnStructureDestroy?.Invoke(structure);
            Structures.Remove(structure);
            Controls.RemoveAll(x => structure.GetBlocksByType<ICharacterInterface>().Contains(x));
            Updatables.RemoveAll(x => structure.GetBlocksByType<IUpdatableBlock>().Contains(x));
            PowerUsers.RemoveAll(x => structure.GetBlocksByType<IPowerUser>().Contains(x));
            FuelUsers.RemoveAll(x => structure.GetBlocksByType<IFuelUser>().Contains(x));
            ForceUsers.RemoveAll(x => structure.GetBlocksByType<IForceUser>().Contains(x));
        }


        private void Update()
        {
            DeltaTime = Time.deltaTime;
            FirstPersonController player = Session.Instance.Player;
            /*foreach (IStructure str in Structures)
            {
                float radius = str.Radius;
                
                float distToPlayer = (player.transform.position - str.transform.position).sqrMagnitude;
                distToPlayer -= radius;
                float[] distances = GameData.Data.sqrLodDistances;
                for (int i = 0; i < distances.Length; i++)
                {
                    if (distToPlayer <= distances[i])
                    {
                        str.UpdateStructureLod(i, player.transform.position);
                        break;
                    }
                }

            }*/
            foreach (ICharacterInterface t in Controls)
            {
                IStructure str = t.Structure;
                if (str.Active && t.GetAttachedControllersCount > 0 && t.IsActive)
                {
                    t.ReadInput(); //TODO: continue to read input to times when axis released
                }
            }
            isConsumptionTick = true;
            OnBeginConsumptionTick?.Invoke();
            foreach (IPowerUser t in PowerUsers)
            {
                IStructure str = t.Structure;
                if (str.Active && t.IsActive)
                {
                    t.ConsumptionTick();
                }
            }
            OnConsumptionTickEnd?.Invoke();
            isConsumptionTick = false;
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
                    t.UpdateBlock(0);
                }
            }
            OnEndUpdateTick?.Invoke();
        }

        private void FixedUpdate()
        {
            if(!Physics.autoSimulation) return;
            DeltaTime = Time.deltaTime;
            for (int i = 0; i < ForceUsers.Count; i++)
            {
                IStructure str = ForceUsers[i].Structure;
                if (str.Active && ForceUsers[i].IsActive)
                {
                    ForceUsers[i].ApplyForce();
                }
            }
            OnEndPhysicsTick?.Invoke();
        }
    }

    public static class Extensions
    {
        public static float DeltaTime(this float value)
        {
            return value * StructureUpdateModule.DeltaTime;
        }

        public static Vector3 DeltaTime(this Vector3 value)
        {
            return value * StructureUpdateModule.DeltaTime;
        }
    }
    
}
