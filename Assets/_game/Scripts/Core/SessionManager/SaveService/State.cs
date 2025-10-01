using System.Collections.Generic;
using Core.ContentSerializer;
using Core.ContentSerializer.Bundles;
using Core.ContentSerializer.Providers;
using Core.Structure;
using Core.World;

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

        public State(IEnumerable<StructureEntity> structureEntities)
        {
            structuresCache = new();
            var serializer = new Serializer(new StructureProvider.StructureBehaviour());
            foreach (var entity in structureEntities)
            {
                structuresCache.Add(new StructureBundle(entity.Structure, serializer));
            }
        }
    }
}
