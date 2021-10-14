using System;
using System.Collections.Generic;
using Core.Structure.Rigging.Storage;
using Runtime;
using UnityEngine;

namespace Core.Structure.Rigging
{
    public static class Utilities
    {
        public static void CalculateFuelTick(IStorage storage) => FuelTicks[(int)storage.Mode](storage);

        private static readonly List<Action<IStorage>> FuelTicks = new List<Action<IStorage>>
        {
            AutoFuelTick,
            PullFuelTick,
            PushFuelTick
        };

        private static void AutoFuelTick(IStorage storage)
        {
            float delta = GameData.Data.fuelTransitionAmount - storage.AmountInPort;
            if (delta.Equals(0f)) return;
            storage.PushToPort(delta);
        }
        
        private static void PullFuelTick(IStorage storage)
        {
            if (storage.AmountInPort.Equals(0f)) return;
            storage.PushToPort(-storage.AmountInPort);
        }
        
        private static void PushFuelTick(IStorage storage)
        {
            float delta = (GameData.Data.fuelTransitionAmount + storage.MaxOutput) - storage.AmountInPort;
            if (delta.Equals(0f)) return;
            storage.PushToPort(delta);
        }

        private static List<string> _possibleTypes;

        public static IEnumerable<string> GetPossibleTypes()
        {
            if (_possibleTypes == null)
            {
                _possibleTypes = new List<string>();
                _possibleTypes.Add("Null");
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (System.Reflection.Assembly assembly in assemblies)
                {
                    foreach (System.Type type in assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(StorageItem)))
                        {
                            _possibleTypes.Add(type.Name);
                        }
                    }
                }
            }

            return _possibleTypes;
        }
    }
}
