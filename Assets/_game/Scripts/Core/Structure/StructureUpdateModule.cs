using System;
using System.Collections.Generic;
using Core.Structure.Rigging;
using Core.Utilities;

namespace Core.Structure
{
    public class StructureUpdateModule : Singleton<StructureUpdateModule>
    {
        public static List<IStructure> Structures = new List<IStructure>();
        public static List<IControl> Controls = new List<IControl>();
        public static List<IUpdatableBlock> Updatables = new List<IUpdatableBlock>();
        public static List<IPowerUser> PowerUsers = new List<IPowerUser>();
        public static List<IFuelUser> FuelUsers = new List<IFuelUser>();
        public static List<IForceUser> ForceUsers = new List<IForceUser>();

        public static bool isConsumptionTick = false;
        public static event Action onConsumptionTickEnd;
        public static event Action onBeginConsumptionTick;

        protected override void Setup()
        {
            onConsumptionTickEnd = null;
            onBeginConsumptionTick = null;
        }
        
        public static void RegisterStructure(IStructure structure)
        {
            Structures.Add(structure);
            Controls.AddRange(structure.GetBlocksByType<IControl>());
            Updatables.AddRange(structure.GetBlocksByType<IUpdatableBlock>());
            PowerUsers.AddRange(structure.GetBlocksByType<IPowerUser>());
            FuelUsers.AddRange(structure.GetBlocksByType<IFuelUser>());
            ForceUsers.AddRange(structure.GetBlocksByType<IForceUser>());
        }

        public static void DestroyStructure(IStructure structure)
        {
            Structures.Remove(structure);
            Controls.RemoveAll(x => structure.GetBlocksByType<IControl>().Contains(x));
            Updatables.RemoveAll(x => structure.GetBlocksByType<IUpdatableBlock>().Contains(x));
            PowerUsers.RemoveAll(x => structure.GetBlocksByType<IPowerUser>().Contains(x));
            FuelUsers.RemoveAll(x => structure.GetBlocksByType<IFuelUser>().Contains(x));
            ForceUsers.RemoveAll(x => structure.GetBlocksByType<IForceUser>().Contains(x));
        }


        private void Update()
        {
            foreach (IControl t in Controls)
            {
                IStructure str = t.Structure;
                if (str.Active && str.enabled && t.IsUnderControl)
                {
                    t.ReadInput();
                }
            }
            foreach (IUpdatableBlock t in Updatables)
            {
                IStructure str = t.Structure;
                if (str.Active && str.enabled)
                {
                    t.UpdateBlock();
                }
            }
            isConsumptionTick = true;
            onBeginConsumptionTick?.Invoke();
            foreach (IPowerUser t in PowerUsers)
            {
                IStructure str = t.Structure;
                if (str.Active && str.enabled)
                {
                    t.ConsumptionTick();
                }
            }
            onConsumptionTickEnd?.Invoke();
            isConsumptionTick = false;
            foreach (IPowerUser t in PowerUsers)
            {
                IStructure str = t.Structure;
                if (str.Active && str.enabled)
                {
                    t.PowerTick();
                }
            }
            foreach (IFuelUser t in FuelUsers)
            {
                IStructure str = t.Structure;
                if (str.Active && str.enabled)
                {
                    t.FuelTick();
                }
            }

        }

        private void FixedUpdate()
        {
            for (int i = 0; i < ForceUsers.Count; i++)
            {
                IStructure str = ForceUsers[i].Structure;
                if (str.Active && str.enabled)
                {
                    ForceUsers[i].ApplyForce();
                }
            }
        }
    }
}
