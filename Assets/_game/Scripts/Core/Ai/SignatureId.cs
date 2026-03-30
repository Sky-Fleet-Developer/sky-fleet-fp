using System;
using System.Collections.Generic;
using Core.Misc;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Ai
{
    [Serializable]
    public struct SignatureId
    {
        [ValueDropdown("GetAbleSignatures")]
        [SerializeField] private string signature;

        [ShowInInspector]
        private string SignatureManual
        {
            get => signature;
            set => signature = value;
        }
        
        public static implicit operator string(SignatureId signature) => signature.signature;
        
        private IEnumerable<string> GetAbleSignatures()
        {
            return EditorReferences.RelationsTableEditor.GetAllRegisteredSignatures();
        }
    }
}