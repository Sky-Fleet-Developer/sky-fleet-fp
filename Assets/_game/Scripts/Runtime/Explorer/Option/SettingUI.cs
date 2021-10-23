using Core.GameSetting;
using Core.UiStructure;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer
{
    public class SettingUI : UiBlockBase
    {

        [SerializeField] private ItemPointer prefabCategory;
        [SerializeField] private ItemPointer prefabButton;

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
            ControlSetting control = SettingManager.Instance.GetControlSetting();
            foreach (ControlSetting.CategoryInputs category in control.Categoryes)
            {
                ItemPointer categoryItem = DynamicPool.Instance.Get(prefabCategory, content);
                categoryItem.GetPointer<Text>("Text").text = category.Name;
                items.AddLast(categoryItem);
                foreach (InputAbstractType input in category.Inputs)
                {
                    ItemPointer inputItem = DynamicPool.Instance.Get(prefabButton, content);
                    inputItem.GetPointer<Text>("Name").text = input.Name;
                    if (input.GetTypeInput() == TypeInput.InputAxis)
                    {
                        InputAxis axis = (InputAxis)input;
                        inputItem.GetPointer<Text>("InputsList").text = GetListInput(input);
                        inputItem.GetPointer<Button>("AddKey").onClick.AddListener(delegate { CallAddInputAxis(axis, inputItem); });
                    }
                    else
                    {
                        InputButtons button = (InputButtons)input;
                        inputItem.GetPointer<Text>("InputsList").text = GetListInput(input);
                        inputItem.GetPointer<Button>("AddKey").onClick.AddListener(delegate { CallAddInputButton(button, inputItem); });
                    }
                    inputItem.GetPointer<Button>("ClearButton").onClick.AddListener(delegate { CallClearInput(input, inputItem); });

                }
            }
        }

        private string GetListInput(InputAbstractType input)
        {
            if (input.GetTypeInput() == TypeInput.InputAxis)
            {
                return ((InputAxis)input).ToString();
            }
            else
            {
                InputButtons button = (InputButtons)input;
                StringBuilder listButtons = new StringBuilder();
                for (int i = 0; i < button.Keys.Count; i++)
                {
                    listButtons.Append(button.Keys[i].ToString());
                    listButtons.Append(" ");
                }
                return listButtons.ToString();
            }
        }

        private void CallClearInput(InputAbstractType input, ItemPointer pointerUI)
        {
            input.Clear();
            pointerUI.GetPointer<Text>("InputsList").text = GetListInput(input);
        }

        private void CallAddInputButton(InputButtons input, ItemPointer pointerUI)
        {
            TakeInputUI.Instance.GetInputButtons(x => { OnAddInputButton(x, input, pointerUI); });
            pointerUI.GetPointer<Button>("AddKey").interactable = false;
        }

        private void CallAddInputAxis(InputAxis input, ItemPointer pointerUI)
        {
            TakeInputUI.Instance.GetInputAxis(x => { OnAddInputAxis(x, input, pointerUI); });
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
            SettingManager.Instance.SaveSetting();
        }
    }
}