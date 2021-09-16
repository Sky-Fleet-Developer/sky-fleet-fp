using System;
using System.Collections;
using System.Collections.Generic;
using Structure.Rigging;
using UnityEngine;

namespace Structure
{
    public class StructureManager : Singleton<StructureManager>
    {
        public static List<IStructure> Structures = new List<IStructure>();
        public static List<IControl> Controls = new List<IControl>();
        public static List<IPowerUser> PowerUsers = new List<IPowerUser>();
        public static List<IFuelUser> FuelUsers = new List<IFuelUser>();
        public static List<IForceUser> ForceUsers = new List<IForceUser>();
        
        public static void RegisterStructure(IStructure structure)
        {
            Structures.Add(structure);
            Controls.AddRange(structure.GetBlocksByType<IControl>());
            PowerUsers.AddRange(structure.GetBlocksByType<IPowerUser>());
            FuelUsers.AddRange(structure.GetBlocksByType<IFuelUser>());
            ForceUsers.AddRange(structure.GetBlocksByType<IForceUser>());
        }

        public static void DestroyStructure(IStructure structure)
        {
            Structures.Remove(structure);
            Controls.RemoveAll(x => structure.GetBlocksByType<IControl>().Contains(x));
            PowerUsers.RemoveAll(x => structure.GetBlocksByType<IPowerUser>().Contains(x));
            FuelUsers.RemoveAll(x => structure.GetBlocksByType<IFuelUser>().Contains(x));
            ForceUsers.RemoveAll(x => structure.GetBlocksByType<IForceUser>().Contains(x));
        }


        private void Update()
        {
            foreach (var t in Controls)
            {
                var str = t.Structure;
                if (str.Active && str.enabled && t.IsUnderControl)
                {
                    t.ReadInput();
                }
            }
            foreach (var t in PowerUsers)
            {
                var str = t.Structure;
                if (str.Active && str.enabled)
                {
                    t.PowerTick();
                }
            }
            foreach (var t in FuelUsers)
            {
                var str = t.Structure;
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
                var str = ForceUsers[i].Structure;
                if (str.Active && str.enabled)
                {
                    ForceUsers[i].ApplyForce();
                }
            }
        }
    }
}
