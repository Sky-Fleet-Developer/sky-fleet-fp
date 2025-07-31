using Core.Utilities;
using UnityEngine;

namespace Core.Structure.Rigging
{
    [System.Serializable]
    public struct CharacterDetachData
    {
        public Transform anchor;
        public DOTweenTransition transition;
    }
}