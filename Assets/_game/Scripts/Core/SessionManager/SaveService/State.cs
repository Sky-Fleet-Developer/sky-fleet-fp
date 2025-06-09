using System.Collections.Generic;
using Core.ContentSerializer;
using Core.ContentSerializer.Bundles;
using Core.ContentSerializer.Providers;
using Core.Structure;

namespace Core.SessionManager.SaveService
{
    [System.Serializable]
    public class State
    {
        public UnityEngine.Vector3 worldOffset;
        public UnityEngine.Vector3 playerPos;
        public UnityEngine.Vector3 playerRot;

        public List<StructureBundle> structuresCache;
        //TODO: characters
        //TODO: session settings

        public State(List<IStructure> structures)
        {
            structuresCache = new(structures.Count);
            var serializer = new Serializer(new StructureProvider.StructureBehaviour());
            foreach (var structure in structures)
            {
                structuresCache.Add(new StructureBundle(structure, serializer));
            }
        }
    }
}
