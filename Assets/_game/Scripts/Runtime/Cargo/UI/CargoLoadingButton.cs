using System;
using Core.UIStructure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Runtime.Cargo.UI
{
    [RequireComponent(typeof(WorldTrackingRect))]
    public class CargoLoadingButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISelectionTarget
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Color normalColor;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color hoverColor;
        public Action<ISelectionTarget> OnSelected { get; set; }
        private Color _initialColor;
        private bool _selected;
        private WorldTrackingRect _worldTrackingRect;

        private void Awake()
        {
            _worldTrackingRect = GetComponent<WorldTrackingRect>();
            _initialColor = targetGraphic.color;
        }

        public void SetTrackingTarget(Transform target)
        {
            _worldTrackingRect.SetTrackingObject(target);
        }

        public void SetText(string text)
        {
            label.text = text;
        }

        public void Selected()
        {
            _selected = true;
        }

        public void Deselected()
        {
            _selected = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSelected?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetGraphic.color = _initialColor * hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetGraphic.color = _initialColor * (_selected ? selectedColor : normalColor);
        }
    }
}