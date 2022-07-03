using System;
using System.Collections.Generic;
using System.Text;
using Core.Data.GameSettings;
using Core.UiStructure;
using Core.UIStructure;
using Core.UIStructure.Utilities;
using Core.Utilities;
using Paterns.AbstractFactory;
using Runtime.Explorer.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.Options
{
    public class SettingUI : Service
    {

        [SerializeField] private ItemPointer prefabCategory;
        [SerializeField] private ItemPointer prefabButton;
        [SerializeField] private ItemPointer prefabAxis;
        [SerializeField] private ItemPointer prefabToggle;

        [SerializeField] private Button saveButton;

        [SerializeField] private Transform content;

        LinkedList<ItemPointer> items = new LinkedList<ItemPointer>();

        private void Start()
        {
            FillList();
            saveButton.onClick.AddListener(CallSaveOption);
        }


        private void FillList()
        {
            ControlSettings control = ControlSettings.Current;
            FactoryUIElement factory = new FactoryUIElement();
            foreach (InputCategory category in control.Categories)
            {
                ItemPointer categoryItem = DynamicPool.Instance.Get(prefabCategory, content);
                categoryItem.GetPointer<Text>("Text").text = category.Name;
                items.AddLast(categoryItem);
                foreach (ElementControlSetting input in category.Elements)
                {
                    ItemPointer item = factory.Generate(new ElementDefine() { SettingElement = input, Basic = this });
                    item.GetPointer<Text>("Name").text = input.Name;
                    items.AddLast(item);
                }
            }
        }

        //Abstract factory/
        private struct ElementDefine
        {
            public ElementControlSetting SettingElement;
            public SettingUI Basic;
        }

        private abstract class GeneratorUIElement<T> : Generator<ElementDefine, ItemPointer>
        {
            public override bool CheckDefine(ElementDefine define)
            {
                return define.SettingElement.GetType() == typeof(T);
            }
        }

        private class FactoryUIElement : AbstractFactory<ElementDefine, ItemPointer>
        {
            public FactoryUIElement()
            {
                RegisterNewType(new InputButtonGenerator());
                RegisterNewType(new InputAxisGenerator());
                RegisterNewType(new ToggleGenerator());
            }


            protected override ItemPointer GetDefault()
            {
                return null;
            }
        }
        //Abstract factory\

        private class InputButtonGenerator : GeneratorUIElement<InputButtons>
        {
            public override ItemPointer Generate(ElementDefine define)
            {
                InputButtons button = (InputButtons)define.SettingElement;
                ItemPointer inputItem = DynamicPool.Instance.Get(define.Basic.prefabButton, define.Basic.content);
                inputItem.GetPointer<Text>("InputsList").text = define.Basic.GetListInput(button);
                inputItem.GetPointer<Button>("AddKey").onClick.AddListener(delegate { define.Basic.CallAddInputButton(button, inputItem); });
                inputItem.GetPointer<Button>("ClearButton").onClick.AddListener(delegate { define.Basic.CallClearInput(button, inputItem); });

                return inputItem;
            }
        }

        private class InputAxisGenerator : GeneratorUIElement<InputAxis>
        {
            public override ItemPointer Generate(ElementDefine define)
            {
                InputAxis axis = (InputAxis)define.SettingElement;
                ItemPointer inputItem = DynamicPool.Instance.Get(define.Basic.prefabAxis, define.Basic.content);
                inputItem.GetPointer<Text>("InputsList").text = define.Basic.GetListInput(axis);
                inputItem.GetPointer<Toggle>("Inversion").isOn = axis.GetAxis().Inverse;
                inputItem.GetPointer<InputField>("Multiply").SetTextWithoutNotify(axis.GetAxis().Multiply.ToString());
                inputItem.GetPointer<Button>("AddKey").onClick.AddListener(delegate { define.Basic.CallAddInputAxis(axis, inputItem); });
                inputItem.GetPointer<Button>("ClearButton").onClick.AddListener(delegate { define.Basic.CallClearInput(axis, inputItem); });
                inputItem.GetPointer<Toggle>("Inversion").onValueChanged.AddListener(x => { axis.SetInverse(x); });
                inputItem.GetPointer<InputField>("Multiply").onValueChanged.AddListener(x =>
                {
                    try
                    {
                        float f = Convert.ToSingle(x);
                        axis.SetMultiply(f);
                    }
                    catch { };

                });
                return inputItem;
            }
        }

        private class ToggleGenerator : GeneratorUIElement<ToggleSetting>
        {
            public override ItemPointer Generate(ElementDefine define)
            {
                ToggleSetting toggle = (ToggleSetting)define.SettingElement;
                ItemPointer inputItem = DynamicPool.Instance.Get(define.Basic.prefabToggle, define.Basic.content);
                inputItem.GetPointer<Toggle>("Toggle").isOn = toggle.IsOn;
                inputItem.GetPointer<Toggle>("Toggle").onValueChanged.AddListener(x => { toggle.IsOn = x; });
                return inputItem;
            }
        }

        private string GetListInput(InputAbstractType input)
        {
            if (input.GetTypeInput() == TypeInput.InputAxis)
            {
                return ((InputAxis)input).GetAxis().ToString();
            }
            else
            {
                InputButtons button = (InputButtons)input;
                StringBuilder listButtons = new StringBuilder();
                for (int i = 0; i < button.Keys.Count; i++)
                {
                    listButtons.Append(button.Keys[i].ToString());
                    if (i < button.Keys.Count - 1)
                    {
                        listButtons.Append(", ");
                    }
                }
                return listButtons.ToString();
            }
        }

        //Other input settings functions
        private void CallClearInput(InputAbstractType input, ItemPointer pointerUI)
        {
            input.Clear();
            pointerUI.GetPointer<Text>("InputsList").text = GetListInput(input);
        }

        private void CallAddInputButton(InputButtons input, ItemPointer pointerUI)
        {
            InputReader reader = ServiceIssue.Instance.GetOrMakeService<InputReader>();
            reader.Window.RectTransform.Fullscreen();
            reader.GetInputButtons(x => { OnAddInputButton(x, input, pointerUI); });
            pointerUI.GetPointer<Button>("AddKey").interactable = false;
        }

        private void CallAddInputAxis(InputAxis input, ItemPointer pointerUI)
        {
            InputReader reader = ServiceIssue.Instance.GetOrMakeService<InputReader>();
            reader.Window.RectTransform.Fullscreen();
            reader.GetInputAxis(x => { OnAddInputAxis(x, input, pointerUI); });
            pointerUI.GetPointer<Button>("AddKey").interactable = false;
        }


        private void OnAddInputButton(ButtonCodes buttonCode, InputButtons input, ItemPointer pointerUI)
        {
            input.AddKey(buttonCode);
            pointerUI.GetPointer<Text>("InputsList").text = GetListInput(input);
            pointerUI.GetPointer<Button>("AddKey").interactable = true;
        }

        private void OnAddInputAxis(AxisCode buttonCode, InputAxis input, ItemPointer pointerUI)
        {
            input.SetAxis(buttonCode);
            pointerUI.GetPointer<Text>("InputsList").text = GetListInput(input);
            pointerUI.GetPointer<Button>("AddKey").interactable = true;
        }
        //
        private void CallSaveOption()
        {
            ControlSettings.Save();
        }
    }
}