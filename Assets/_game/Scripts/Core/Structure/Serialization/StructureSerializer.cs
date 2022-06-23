using System;
using System.Collections.Generic;
using Core.ContentSerializer;
using Core.ContentSerializer.Bundles;
using Core.ContentSerializer.Providers;
using Core.GameSerialization;
using Core.Patterns.State;
using Core.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Structure.Serialization
{
    public class StructureSerializer : ISerializable
    {
        public string ServiceId => "STRUCTURES";
        
        public void PrepareForGameSerialization(State state)
        {
            IEnumerable<IStructure> structures = CollectStructures();

            Serializer serializer = StructureProvider.GetSerializer();

            List<StructureBundle> bundles = serializer.GetBundlesFor(structures);
        }

        public event Action OnDataWasSerialized;
        public void Deserialize(State state)
        {
        }
        
        public void PrepareForGameSerialization(string serializationId)
        {

        }

        private IEnumerable<IStructure> CollectStructures()
        {
            return Application.isPlaying ? CollectInRuntime() : CollectInEditor();
        }

        private IEnumerable<IStructure> CollectInRuntime()
        {
            return StructureUpdateModule.Structures.Clone();
        }

        private IEnumerable<IStructure> CollectInEditor()
        {
            List<IStructure> result = new List<IStructure>();

            foreach (MonoBehaviour monobeh in Object.FindObjectsOfType<MonoBehaviour>())
            {
                if (monobeh is IStructure structure) result.Add(structure);
            }

            return result;
        }
    }
}
