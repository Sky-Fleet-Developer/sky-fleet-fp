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
        
        public void PrepareForGameSerialization(IState state)
        {
            IEnumerable<IStructure> structures = CollectStructures();

            Serializer serializer = StructureProvider.GetSerializer();

            List<StructureBundle> bundles = structures.Select(x => new StructureBundle(x, serializer)).ToList();
        }

        public event Action OnDataWasSerialized;
        public void Deserialize(IState state)
        {
        }
        
        private IEnumerable<IStructure> CollectStructures()
        {
            return Application.isPlaying ? CollectInRuntime() : CollectInEditor();
        }

        private IEnumerable<IStructure> CollectInRuntime()
        {
            for (int i = 0; i < CycleService.Structures.Count; i++)
            {
                yield return CycleService.Structures[i];
            }
        }

        private IEnumerable<IStructure> CollectInEditor()
        {
            return Object.FindObjectsOfType<MonoBehaviour>().OfType<IStructure>();
        }
    }
}
