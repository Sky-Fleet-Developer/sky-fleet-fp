using System;
using Core.UiStructure;
using Core.UIStructure;
using Runtime.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Explorer
{
    [Serializable]
    public class StartMenuItem
    {
        public Service[] blocks;
        public string description;
        [DrawWithUnity] public FontStyle style;
        [DrawWithUnity] public TextAnchor alignment;

        public Action<IService[]> OnBlockWasOpen;
    }
}