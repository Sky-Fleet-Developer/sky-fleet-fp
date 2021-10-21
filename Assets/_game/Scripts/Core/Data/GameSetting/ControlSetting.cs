using System;
using System.Collections;
using System.Collections.Generic;
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
    }

    [System.Serializable]
    public class InputButtons : InputAbstractType
    {


        public List<ButtonInput[]> Keys => keys;

        private List<ButtonInput[]> keys;

        public InputButtons()
        {
            typeInput = TypeInput.InputButtons;
        }

        public void AddKey(ButtonInput[] key)
        {
            keys.Add(key);
        }

        public void SetKey(ButtonInput[] key)
        {
            keys.Clear();
            keys.Add(key);
        }
    }

    [System.Serializable]
    public class InputAxis : InputAbstractType
    {
        public string GetNameAxis() => nameAxis;

        private string nameAxis;

        public InputAxis()
        {
            typeInput = TypeInput.InputAxis;
        }

        public void SetAxis(string name)
        {
            nameAxis = name;
        }

        public bool IsNone()
        {
            return string.IsNullOrEmpty(nameAxis);
        }
    }

    [System.Serializable]
    public class ButtonInput
    {
        public KeyCode GetKeyCode() => keyCode;

        private KeyCode keyCode = KeyCode.None;

        public void Clear()
        {
            keyCode = KeyCode.None;
        }

        public void SetKeyCode(KeyCode key)
        {
            keyCode = key;
        }

        public bool IsNone()
        {
            return keyCode == KeyCode.None;
        }
    }
}