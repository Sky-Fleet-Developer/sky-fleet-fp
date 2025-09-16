using System;

namespace Core.UIStructure.Utilities
{
    public interface ISelectionTarget
    {
        Action<ISelectionTarget> OnSelected { get; set; }
        void Selected();
        void Deselected();
    }
}