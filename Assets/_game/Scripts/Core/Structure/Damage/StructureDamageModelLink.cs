using Core.Utilities;
using UnityEngine;

namespace Core.Structure.Damage
{
    public class StructureDamageModelLink : MonoBehaviour
    {
        private StructureDamageModelPool _myDamageModelPool;
        private IStructure _myStructure;
        private Bounds _bounds;
        
        public StructureDamageModelPool ModelPool => _myDamageModelPool;
        public IStructure Structure => _myStructure;

        public static void CreateForStructure(IStructure structure, StructureDamageModelPool modelPool)
        {
            var link = new GameObject("StructureDamageModelLink").AddComponent<StructureDamageModelLink>();
            link.transform.SetParent(structure.transform);
            link._myDamageModelPool = modelPool;
            link._myStructure = structure;
            link._bounds = structure.transform.GetBounds();
            link.transform.localPosition = link._bounds.center;
            link.transform.localRotation = Quaternion.identity;
            var collider = link.gameObject.AddComponent<BoxCollider>();
            collider.size = link._bounds.size;
            collider.isTrigger = true;
        }
    }
}