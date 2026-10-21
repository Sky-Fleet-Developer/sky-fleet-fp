using System;
using QnE;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Narrative.Authoring
{
    public enum ValueKind
    {
        Bool,
        Int,
        Float,
        String
    }

    /// <summary>An authored blackboard value.</summary>
    [Serializable]
    public class ValueAsset
    {
        [EnableIf(nameof(KindEditable))] public ValueKind kind;
        [SerializeField, HideInInspector] internal bool kindLocked;
        [ShowIf(nameof(IsBool))] public bool boolValue;
        [ShowIf(nameof(IsInt))] public int intValue;
        [ShowIf(nameof(IsFloat))] public float floatValue;
        [ShowIf(nameof(IsString))] public string stringValue;

        private bool KindEditable => !kindLocked;
        private bool IsBool => kind == ValueKind.Bool;
        private bool IsInt => kind == ValueKind.Int;
        private bool IsFloat => kind == ValueKind.Float;
        private bool IsString => kind == ValueKind.String;

        public void SetKind(ValueKind value) => kind = value;

        public QValue Build()
        {
            switch (kind)
            {
                case ValueKind.Bool: return QValue.From(boolValue);
                case ValueKind.Int: return QValue.From(intValue);
                case ValueKind.Float: return QValue.From((double)floatValue);
                default: return QValue.From(stringValue ?? string.Empty);
            }
        }
    }

    [Serializable]
    public class ArgumentAsset
    {
        public string name;
        public ValueAsset value = new();
    }
}
