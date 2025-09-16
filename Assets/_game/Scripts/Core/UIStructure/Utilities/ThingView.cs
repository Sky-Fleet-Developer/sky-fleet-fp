using System;
using UnityEngine;

namespace Core.UIStructure.Utilities
{
    public abstract class ThingView<T> : MonoBehaviour, ISelectionTarget, IMultipleSelectionTarget
    {
        public abstract T Data { get; }
        public int Order => transform.GetSiblingIndex();
        public Action<IMultipleSelectionTarget, MultipleSelectionModifiers> OnInput { get; set; }
        public Action<ISelectionTarget> OnSelected { get; set; }
        public abstract void Selected();
        public abstract void Deselected();

        public abstract void SetData(T data);
        public abstract void RefreshView();
        public abstract void EmitSelection();
    }
}