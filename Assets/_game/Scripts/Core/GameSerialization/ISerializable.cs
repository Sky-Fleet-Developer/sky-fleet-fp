using System;
using Core.Patterns.State;

namespace Core.GameSerialization
{
    public interface ISerializable
    {
        void PrepareForGameSerialization(IState state);
        /// <summary>
        /// format: serialization id, self, data
        /// </summary>
        event Action OnDataWasSerialized;

        void Deserialize(IState state);
    }
}
