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
                        inputItem.GetPointer<Text>("InputsList").text = axis.GetNameAxis();
                    }
                    else
                    {
                        InputButtons button = (InputButtons)input;
                        StringBuilder listButtons = new StringBuilder();
                        for(int i = 0; i < button.Keys.Count;i++)
                        {
                            for (int i2 = 0; i2 < button.Keys[i].Length; i2++)
                            {
                                listButtons.Append(button.Keys[i][i2].GetKeyCode().ToString());
                                if(i2 != button.Keys[i].Length - 1)
                                {
                                    listButtons.Append("+");
                                }
                                listButtons.Append(" ");
                            }
                        }
                        inputItem.GetPointer<Text>("InputsList").text = listButtons.ToString();
                    }
                    inputItem.GetPointer<Button>("ClearButton").onClick.AddListener(delegate { CallClearInput(input); });
                    inputItem.GetPointer<Button>("AddKey").onClick.AddListener(delegate { CallAddInput(input); });
                }
            }
        }

        private void ClearList()
        {

        }

        private void CallClearInput(InputAbstractType input)
        {

        }

        private void CallAddInput(InputAbstractType input)
        {
            InputControl.Instance.TakeInput();
        }

        private void CallUpdateInput(InputAbstractType input)
        {

        }

        private void CallSaveOption()
        {
            SettingManager.Instance.SaveSetting();
        }
    }
}