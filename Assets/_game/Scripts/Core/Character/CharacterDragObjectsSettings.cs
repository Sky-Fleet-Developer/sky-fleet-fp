using System;
using UnityEngine;

namespace Core.Character
{
    [Serializable]
    public class CharacterDragObjectsSettings
    {
        public float maxPullForce;
        public float maxPullDistance;
        public float disruptionDistance;
        public AnimationCurve pullCurve;
        public float pullUpRate;
        public float breakForceRate;
    }
}