using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Core.GameSetting
{
    [System.Serializable]
    public class ControlSetting
    {
        public class CategoryInputs : INameSetting
        {
            public string Name { get; set; }

            public List<ElementControlSetting> Elements { get; private set; }

            public CategoryInputs()
            {
                Elements = new List<ElementControlSetting>();
            }

            public InputAxis AddAxisInput(string name)
            {
                InputAxis axis = new InputAxis();
                axis.Name = name;
                Elements.Add(axis);
                return axis;
            }

            public InputButtons AddInputButtons(string name)
            {
                InputButtons buttons = new InputButtons();
                buttons.Name = name;
                Elements.Add(buttons);
                return buttons;
            }

            public ToggleSetting AddToggle(string name)
            {
                ToggleSetting toggle = new ToggleSetting();
                toggle.Name = name;
                Elements.Add(toggle);
                return toggle;
            }

            public T FindElement<T>(string name) where T : ElementControlSetting
            {
                return (T)Elements.Where(x => { return x.Name == name && x.GetType() == typeof(T); }).FirstOrDefault();
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
            moveCategory.AddToggle("Use axles for move player?");
            moveCategory.AddAxisInput("Axis forward/back");
            moveCategory.AddAxisInput("Axis left/right");
            CategoryInputs cameraCategory = control.AddCategory("Camera");
            cameraCategory.AddAxisInput("Axis up/down");
            cameraCategory.AddAxisInput("Axis left/right");

            CategoryInputs generalCategory = control.AddCategory("General");
            generalCategory.AddInputButtons("Fast save");
            generalCategory.AddInputButtons("Set pause");
            return control;
        }

    }

    public interface INameSetting
    {
        string Name { get; set; }
    }
    

    public abstract class ElementControlSetting : INameSetting
    {
        public string Name { get; set; }
    }

    public class ToggleSetting : ElementControlSetting
    {
        public bool IsOn { get; set; }
    }

    //Input Types

    public enum TypeInput
    {
        InputButtons = 0,
        InputAxis = 1,
    }

    public abstract class InputAbstractType : ElementControlSetting
    {
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

        [SerializeField] private List<ButtonCodes> keys;

        public InputButtons()
        {
            keys = new List<ButtonCodes>();
            typeInput = TypeInput.InputButtons;
        }

        public void AddKey(ButtonCodes key)
        {
            int exist = keys.Count(x =>
            {
                if (key.KeyCodes.Length != x.KeyCodes.Length) return false;
                bool match = true;
                for (var i = 0; i < key.KeyCodes.Length; i++)
                {
                    match &= key.KeyCodes[i] == x.KeyCodes[i];
                }

                return match;
            });
            if (exist > 0) return;
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
            return keys.Count == 0;
        }
    }

    [System.Serializable]
    public struct AxisCode
    {
        [ShowInInspector]
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

        [SerializeField, HideInInspector]
        private string _name;

        public bool IsAbsolute { get; private set; }

        public bool Inverse;

        public float Multiply;

        public AxisCode(string Name)
        {
            _name = Name;
            IsAbsolute = false;
            Inverse = false;
            Multiply = 1;
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
            axis.Multiply = 1;
            typeInput = TypeInput.InputAxis;
        }

        public void SetAxis(AxisCode name)
        {
            axis = name;
        }

        public void SetMultiply(float val)
        {
            axis.Multiply = val;
        }

        public void SetInverse(bool val)
        {
            axis.Inverse = val;
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
        public KeyCode[] KeyCodes;

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