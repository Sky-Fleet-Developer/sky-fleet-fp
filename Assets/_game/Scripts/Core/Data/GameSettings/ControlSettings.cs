using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.SessionManager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Data.GameSettings
{
    [System.Serializable]
    public class ControlSettings
    {
        public static ControlSettings Current => Session.Instance.Control;
        public List<InputCategory> Categories => categoryInputs;

        private List<InputCategory> categoryInputs;

        public ControlSettings()
        {
            categoryInputs = new List<InputCategory>();
        }

        public InputCategory AddCategory(string name)
        {
            InputCategory inputCategory = new InputCategory();
            inputCategory.Name = name;
            categoryInputs.Add(inputCategory);
            return inputCategory;
        }

        public static ControlSettings GetDefaultSetting()
        {
            ControlSettings control = new ControlSettings();
            InputCategory moveCategory = control.AddCategory("Move player");
            moveCategory.AddInputButtons("Move forward");
            moveCategory.AddInputButtons("Move back");
            moveCategory.AddInputButtons("Move left");
            moveCategory.AddInputButtons("Move right");
            moveCategory.AddInputButtons("Jump");
            moveCategory.AddToggle("Use axles for move player?");
            moveCategory.AddAxisInput("Axis forward/back");
            moveCategory.AddAxisInput("Axis left/right");
            InputCategory cameraCategory = control.AddCategory("Camera");
            cameraCategory.AddAxisInput("Axis up/down").SetAbsolute(false);
            cameraCategory.AddAxisInput("Axis left/right").SetAbsolute(false);

            InputCategory generalCategory = control.AddCategory("General");
            generalCategory.AddInputButtons("Fast save");
            generalCategory.AddInputButtons("Set pause");
            return control;
        }

        public static void Save()
        {
            Session.Instance.SaveControlSetting();
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

        public bool IsAbsolute { get; set; }

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
        
        public void SetAbsolute(bool val)
        {
            axis.IsAbsolute = val;
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
        [DrawWithUnity]
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