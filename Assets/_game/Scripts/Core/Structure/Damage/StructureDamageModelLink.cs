using Core.Utilities;
using UnityEngine;

namespace Core.Structure.Damage
{
    public class StructureDamageModelLink : MonoBehaviour
    {
        private StructureDamageModel _myDamageModel;
        private IStructure _myStructure;
        private Bounds _bounds;
        
        public StructureDamageModel Model => _myDamageModel;
        public IStructure Structure => _myStructure;

        public static void CreateForStructure(IStructure structure, StructureDamageModel model)
        {
            var link = new GameObject("StructureDamageModelLink").AddComponent<StructureDamageModelLink>();
            link.transform.SetParent(structure.transform);
            link.transform.localPosition = Vector3.zero;
            link.transform.localRotation = Quaternion.identity;
            link._myDamageModel = model;
            link._myStructure = structure;
            link._bounds = structure.transform.GetBounds();
        }
    }
}