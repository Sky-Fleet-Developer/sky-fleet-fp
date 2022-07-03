using System.Collections.Generic;
using Core.ContentSerializer.Bundles;

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

        public State()
        {
            
        }
    }
}
