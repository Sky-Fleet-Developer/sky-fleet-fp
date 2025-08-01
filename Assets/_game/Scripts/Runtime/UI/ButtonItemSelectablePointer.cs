using Runtime.UI;
using UnityEngine;

namespace Core.UIStructure.Utilities
{
    public class ButtonItemSelectablePointer : ButtonItemPointer
    {
        [SerializeField] private GameObject maskSelected;

        private bool isSelected;

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                UpdateStateSelected();
            }
        }

        protected override void Awake()
        {
            IsSelected = false;
            base.Awake();
        }

        private void UpdateStateSelected()
        {
            if (isSelected)
            {
                maskSelected.SetActive(true);
            }
            else
            {
                maskSelected.SetActive(false);
            }
        }
    }
}