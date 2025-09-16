using System;
using UnityEngine;

namespace Core.UIStructure.Utilities
{
    [Flags]
    public enum MultipleSelectionModifiers
    {
        None = 0,
        Shift = 1,
        Ctrl = 2
    }
    public interface IMultipleSelectionTarget
    {
        int Order { get; }
        Action<IMultipleSelectionTarget, MultipleSelectionModifiers> OnInput { get; set; }
        void Selected();
        void Deselected();
    }

    public static class MultipleSelectionModifiersExtension
    {
        public static MultipleSelectionModifiers GetFromInput(this MultipleSelectionModifiers value)
        {
            value = MultipleSelectionModifiers.None;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                value |= MultipleSelectionModifiers.Shift;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                value |= MultipleSelectionModifiers.Ctrl;
            }

            return value;
        }
    }
}