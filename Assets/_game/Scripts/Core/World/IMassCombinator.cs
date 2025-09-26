using System;

namespace Core.World
{
    public interface IMassCombinator
    {
        void SetMassDirty(IMassModifier massModifier);
        event Action OnMassChanged;
    }
}