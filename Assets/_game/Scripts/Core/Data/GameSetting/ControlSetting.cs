using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Core.GameSetting
{
    [System.Serializable]
    public class ControlSetting
    {
        public class CategoryInputs : INameSetting
        {
            public string Name { get; set; }

            public List<InputAbstractType> Inputs { get; private set; }

            public CategoryInputs()
            {
                Inputs = new List<InputAbstractType>();
            }

            public InputAxis AddAxisInput(string name)
            {
                InputAxis axis = new InputAxis();
                axis.Name = name;
                Inputs.Add(axis);
                return axis;
            }

            public InputButtons AddInputButtons(string name)
            {
                InputButtons buttons = new InputButtons();
                buttons.Name = name;
                Inputs.Add(buttons);
                return buttons;
            }
        }

        public List<CategoryInputs> Categoryes => categoryInputs;

        private List<CategoryInputs> categoryInputs;

        public ControlSetting()
        {
            categoryInputs = new List<CategoryInputs>();
        }

        public CategoryInputs AddCategory(string name)
        {
            CategoryInputs category = new CategoryInputs();
            category.Name = name;
            categoryInputs.Add(category);
            return category;
        }

        public static ControlSetting GetDefaultSetting()
        {
            ControlSetting control = new ControlSetting();
            CategoryInputs moveCategory = control.AddCategory("Move player");
            moveCategory.AddInputButtons("Move forward");
            moveCategory.AddInputButtons("Move back");
            moveCategory.AddInputButtons("Move left");
            moveCategory.AddInputButtons("Move right");
            moveCategory.AddInputButtons("Jump");
            return control;
        }

    }

    public interface INameSetting
    {
        string Name { get; set; }
    }


    public enum TypeInput
    {
        InputButtons = 0,
        InputAxis = 1,
    }

    public class InputAbstractType : INameSetting
    {
        public string Name { get; set; }

        protected TypeInput typeInput;
        public TypeInput GetTypeInput() => typeInput;

        public virtual void Clear()
        {
        }

        public virtual bool IsNone()
        {
            return true;
        }
    }

    [System.Serializable]
    public class InputButtons : InputAbstractType
    {
        public List<ButtonCodes> Keys => keys;

        private List<ButtonCodes> keys;

        public InputButtons()
        {
            keys = new List<ButtonCodes>();
            typeInput = TypeInput.InputButtons;
        }

        public void AddKey(ButtonCodes key)
        {
            keys.Add(key);
        }

        public void SetKey(ButtonCodes key)
        {
            keys.Clear();
            keys.Add(key);
        }

        public override void Clear()
        {
            keys.Clear();
        }

        public override bool IsNone()
        {
            return keys.Count > 0;
        }
    }

    [System.Serializable]
    public struct AxisCode
    {
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                if (value == "Mouse X" || value == "Mouse Y" || value == "Mouse ScrollWheel")
                {
                    IsAbsolute = true;
                }
                else
                {
                    IsAbsolute = false;
                }
            }
        }

        private string _name;

        public bool IsAbsolute { get; private set; }

        public AxisCode(string Name)
        {
            _name = Name;
            IsAbsolute = false;
            this.Name = Name;
            
        }

        public static AxisCode Zero()
        {
            return new AxisCode("");
        }

        public override string ToString()
        {
            return _name;
        }
    }

    [System.Serializable]
    public class InputAxis : InputAbstractType
    {
        public AxisCode GetAxis() => axis;

        private AxisCode axis;

        public InputAxis()
        {
            axis = new AxisCode();
            typeInput = TypeInput.InputAxis;
        }

        public void SetAxis(AxisCode name)
        {
            axis = name;
        }

        public override bool IsNone()
        {
            return string.IsNullOrEmpty(axis.Name);
        }

        public override void Clear()
        {
            axis.Name = "";
        }
    }

    [System.Serializable]
    public struct ButtonCodes
    {
        public KeyCode[] KeyCodes { get; set; }

        public ButtonCodes(KeyCode[] keys)
        {
            this.KeyCodes = keys;
        }

        public void Clear()
        {
            KeyCodes = new KeyCode[0];
        }

        public bool IsNone()
        {
            return KeyCodes.Length == 0;
        }

        public static ButtonCodes Zero()
        {
            return new ButtonCodes(new KeyCode[0]);
        }

        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            for(int i = 0; i < KeyCodes.Length; i++)
            {
                text.Append(KeyCodes[i].ToString());
                if(i < KeyCodes.Length-1)
                {
                    text.Append('+');
                }
            }
            return text.ToString();
        }
    }
}