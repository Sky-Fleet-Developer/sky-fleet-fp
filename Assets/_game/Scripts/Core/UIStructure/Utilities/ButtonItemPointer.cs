using System;
using Core.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UIStructure.Utilities
{
    public class ButtonItemPointer : ItemPointer
    {
        public Text description;
        public Button button;
        public Image background;

        private Action _callback;
        private object[] defaultArguments = new object[6];

        protected override void Awake()
        {
            defaultArguments[0] = description.text;
            defaultArguments[1] = description.fontSize;
            defaultArguments[2] = description.fontStyle;
            defaultArguments[3] = description.alignment;
            defaultArguments[4] = background.sprite;
            defaultArguments[5] = (background.color, description.color);
            base.Awake();
        }

        public override void SetVisual(params object[] args)
        {
            foreach (object argument in args)
            {
                switch (argument)
                {
                    case string desc:
                        description.text = desc;
                        break;
                    case int font:
                        description.fontSize = font;
                        break;
                    case TextAnchor alignment:
                        description.alignment = alignment;
                        break;
                    case FontStyle style:
                        description.fontStyle = style;
                        break;
                    case Action cb:
                        _callback = cb;
                        break;
                    case Sprite sprite:
                        background.sprite = sprite;
                        break;
                    case Tuple<Color, Color>(var backColor, var descrColor):
                        background.color = backColor;
                        description.color = descrColor;
                        break;
                }
            }
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClick);
        }

        public void ResetVisual()
        {
            SetVisual(defaultArguments);
        }

        private void OnClick()
        {
            _callback?.Invoke();
        }
    }

}