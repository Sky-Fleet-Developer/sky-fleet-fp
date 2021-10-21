using Core.GameSetting;
using Core.UiStructure;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer
{
    public class SettingUI : UiBlockBase
    {
        [SerializeField] private ItemPointer prefabCategory;
        [SerializeField] private ItemPointer prefabButton;

        [SerializeField] private Transform content;

        LinkedList<ItemPointer> items = new LinkedList<ItemPointer>();

        private void Start()
        {
            FillList();
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
                    }
                }
            }
        }

        private void ClearList()
        {

        }

        private void CallClearButton(InputButtons input)
        {

        }

        private void CallAddButton(InputButtons input)
        {

        }
    }
}