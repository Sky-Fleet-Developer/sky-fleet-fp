using Core.Boot_strapper;
using Core.GameSetting;
using Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer
{
    public class TakeInputUI : Singleton<TakeInputUI>, ILoadAtStart
    {
        [SerializeField] private GameObject basic;

        [Header("Dialog take buttons")]
        [SerializeField] private GameObject takeButtons;

        [Header("Dialog take axles")]
        [SerializeField] private GameObject takeAxles;

        [Header("Dialog: what next?")]
        [SerializeField] private GameObject whatNext;
        [SerializeField] private Text nameGetInput;
        [SerializeField] private Button restartBut;
        [SerializeField] private Button cancleBut;
        [SerializeField] private Button okBut;

        private bool IsBusy;

        public Task Load()
        {
            IsBusy = false;
            basic.SetActive(false);
            return Task.CompletedTask;
        }

        public void GetInputButtons(Action<ButtonCodes> endTakeButtons)
        {
            if (IsBusy)
                throw new MethodAccessException();
            IsBusy = true;
            basic.SetActive(true);
            takeButtons.SetActive(true);
            InputControl.Instance.TakeInputButton(x =>
            {
                OnTakeInputButtons(x, endTakeButtons);
            });
        }

        public void GetInputAxis(Action<AxisCode> endTakeAxis)
        {
            if (IsBusy)
                throw new MethodAccessException();
            IsBusy = true;
            basic.SetActive(true);
            takeAxles.SetActive(true);
            InputControl.Instance.TakeInputAxis(x =>
            {
                OnTakeInputAxis(x, endTakeAxis);
            });
        }

        private void OnTakeInputButtons(ButtonCodes buttons, Action<ButtonCodes> endTakeButtons)
        {
            takeButtons.SetActive(false);
            ShowDialog(
                buttons.ToString(),
                delegate
                {
                    IsBusy = false;
                    GetInputButtons(endTakeButtons);
                },
                delegate
                {
                    IsBusy = false;
                    endTakeButtons?.Invoke(ButtonCodes.Zero());
                    basic.SetActive(false);
                },
                delegate
                {
                    IsBusy = false;
                    endTakeButtons?.Invoke(buttons);
                    basic.SetActive(false);
                }
                );
        }

        private void OnTakeInputAxis(AxisCode axis, Action<AxisCode> endTakeAxis)
        {
            takeAxles.SetActive(false);
            ShowDialog(
                axis.ToString(),
                delegate
                {
                    IsBusy = false;
                    GetInputAxis(endTakeAxis);
                },
                delegate
                {
                    IsBusy = false;
                    endTakeAxis?.Invoke(AxisCode.Zero());
                    basic.SetActive(false);
                },
                delegate
                {
                    IsBusy = false;
                    endTakeAxis?.Invoke(axis);
                    basic.SetActive(false);
                }
                );
        }


        private void ShowDialog(string text, Action pressRestart, Action pressCancle, Action pressOk)
        {
            nameGetInput.text = text;
            whatNext.SetActive(true);
            restartBut.onClick.AddListener(delegate { HideDialog(); pressRestart?.Invoke(); });
            cancleBut.onClick.AddListener(delegate { HideDialog(); pressCancle?.Invoke(); });
            okBut.onClick.AddListener(delegate { HideDialog(); pressOk?.Invoke(); });
        }


        private void HideDialog()
        {
            whatNext.SetActive(false);
            restartBut.onClick.RemoveAllListeners();
            cancleBut.onClick.RemoveAllListeners();
            okBut.onClick.RemoveAllListeners();
        }
    }
}