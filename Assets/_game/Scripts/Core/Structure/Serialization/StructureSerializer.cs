using System;
using System.Collections.Generic;
using System.Linq;
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
            IEnumerable<BaseStructure> structures = CollectStructures();

            Serializer serializer = StructureProvider.GetSerializer();

            List<StructureBundle> bundles = structures.Select(x => new StructureBundle(x, serializer)).ToList();

        }

        public event Action OnDataWasSerialized;
        public void Deserialize(State state)
        {
        }
        
        public void PrepareForGameSerialization(string serializationId)
        {

        }

        private IEnumerable<BaseStructure> CollectStructures()
        {
            return Application.isPlaying ? CollectInRuntime() : CollectInEditor();
        }

        private IEnumerable<BaseStructure> CollectInRuntime()
        {
            for (int i = 0; i < StructureUpdateModule.Structures.Count; i++)
            {
                if (StructureUpdateModule.Structures[i] is BaseStructure baseStructure)
                {
                    yield return baseStructure;
                }
            }
        }

        private IEnumerable<BaseStructure> CollectInEditor()
        {
            return Object.FindObjectsOfType<BaseStructure>();
        }
    }
}
