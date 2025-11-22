using System.Collections.Generic;
using Core.Items;
using Core.Trading;
using Zenject;

namespace Runtime.Trading
{
    public class MassAndVolumeCalculator : IMassAndVolumeCalculator
    {
        [Inject] private BankSystem _bankSystem;
        
        public float GetMass(IItemInstancesSourceReadonly source)
        {
            float mass = 0;
            foreach (var itemInstance in source.GetItems())
            {
                mass += GetMass(itemInstance);
            }
            return mass;
        }

        public float GetVolume(IItemInstancesSourceReadonly source)
        {
            float volume = 0;
            foreach (var itemInstance in source.GetItems())
            {
                volume += GetVolume(itemInstance);
            }
            return volume;
        }

        public void GetMassAndVolume(IItemInstancesSourceReadonly source, out float mass, out float volume)
        {
            volume = 0;
            mass = 0;
            foreach (var itemInstance in source.GetItems())
            {
                GetMassAndVolume(itemInstance, out float mass2, out float volume2);
                volume += volume2;
                mass += mass2;
            }
        }

        public float GetMass(ItemInstance item)
        {
            float mass = item.GetMass();
            if (item.TryGetContainerKey(out string key))
            {
                foreach (var child in _bankSystem.GetOrCreateInventory(key).GetItems())
                {
                    mass += GetMass(child);
                }
            }
            return mass;
        }

        public float GetVolume(ItemInstance item)
        {
            if (item.Sign.HasTag(ItemSign.FoldableTag))
            {
                float volume = item.GetVolume();
                if (item.TryGetContainerKey(out string key))
                {
                    foreach (var child in _bankSystem.GetOrCreateInventory(key).GetItems())
                    {
                        volume += GetVolume(child);
                    }
                }

                return volume;
            }
            else
            {
                return item.GetVolume();
            }
        }

        public void GetMassAndVolume(ItemInstance item, out float mass, out float volume)
        {
            volume = item.GetVolume();
            mass = item.GetMass();
            if (item.TryGetContainerKey(out string key))
            {
                foreach (var child in _bankSystem.GetOrCreateInventory(key).GetItems())
                {
                    GetMassAndVolume(child, out float mass2, out float volume2);
                    volume += volume2; mass += mass2;
                }
            }
        }
        
    }
}