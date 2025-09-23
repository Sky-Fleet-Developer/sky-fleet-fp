using System;

namespace Core.Game
{
    public interface IMassCombinator
    {
        void SetMassDirty(IMassModifier massModifier);
        event Action OnMassChanged;
    }
}