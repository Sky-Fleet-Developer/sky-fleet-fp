using System;
using System.Collections.Generic;
using Core.Data;
using Runtime;

namespace Core.Structure.Rigging
{
    public static partial class Utilities
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

        private const float deltaConsumption = 0.02f;
        
        public static void CalculateConsumerTickA(this IConsumer consumer)
        {
            consumer.Power.charge = 0;
            consumer.Power.maxInput = (0.1f + consumer.Consumption) * StructureUpdateModule.DeltaTime;
            consumer.Power.maxOutput = 0;
        }

        public static bool CalculateConsumerTickB(this IConsumer consumer)
        {
            return true; //consumer.Power.charge >= (consumer.Consumption * StructureUpdateModule.DeltaTime - deltaConsumption * consumer.Consumption) * 0.9f;
        }
    }
}
