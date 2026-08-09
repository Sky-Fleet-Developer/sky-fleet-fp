using System;
using UnityEngine;

namespace Core.Structure
{
    [Serializable]
    public struct StructureConfigurationHead
    {
        [NonSerialized] public GameObject Root;
        public Vector3 position;
        public Quaternion rotation;
        public string bodyGuid;
    }
}