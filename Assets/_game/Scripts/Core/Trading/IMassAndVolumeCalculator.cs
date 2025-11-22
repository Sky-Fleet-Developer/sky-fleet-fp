using Core.Items;

namespace Core.Trading
{
    public interface IMassAndVolumeCalculator
    {
        float GetMass(IItemInstancesSourceReadonly source);
        float GetVolume(IItemInstancesSourceReadonly source);
        void GetMassAndVolume(IItemInstancesSourceReadonly source, out float mass, out float volume);
        float GetMass(ItemInstance item);
        float GetVolume(ItemInstance item);
        void GetMassAndVolume(ItemInstance item, out float mass, out float volume);
    }
}