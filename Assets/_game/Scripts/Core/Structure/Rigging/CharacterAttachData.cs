using Core.Utilities;
using UnityEngine;

namespace Core.Structure.Rigging
{
    [System.Serializable]
    public struct CharacterAttachData
    {
        public Transform anchor;
        public bool attachAndLock;
        public DOTweenTransition transition;
    }
}