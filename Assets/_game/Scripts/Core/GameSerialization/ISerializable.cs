using System;
using Core.Patterns.State;

namespace Core.GameSerialization
{
    public interface ISerializable
    {
        void PrepareForGameSerialization(State state);
        /// <summary>
        /// format: serialization id, self, data
        /// </summary>
        event Action OnDataWasSerialized;

        void Deserialize(State state);
    }
}
