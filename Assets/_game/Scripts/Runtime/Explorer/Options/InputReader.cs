using System;
using System.Threading.Tasks;
using Core;
using Core.GameSetting;
using Core.UiStructure;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.Options
{
    public class InputReader : Service
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

        private bool isBusy;

        public Task LoadStart()
        {
            basic.SetActive(false);
            return Task.CompletedTask;
        }

        public void GetInputButtons(Action<ButtonCodes> endTakeButtons)
        {
            if (isBusy) throw new MethodAccessException();
            isBusy = true;
            KeysControl.IsBlocks = true;
            basic.SetActive(true);
            takeButtons.SetActive(true);
            InputControl.Instance.TakeInputButton(x =>
            {
                OnReceiveInput(x, endTakeButtons);
            });
        }

        public void GetInputAxis(Action<AxisCode> endTakeAxis)
        {
            if (isBusy) throw new MethodAccessException();
            isBusy = true;
            KeysControl.IsBlocks = true;
            basic.SetActive(true);
            takeAxles.SetActive(true);
            InputControl.Instance.TakeInputAxis(x =>
            {
                OnReceiveInput(x, endTakeAxis);
            });
        }

        private void OnReceiveInput(ButtonCodes buttons, Action<ButtonCodes> endTakeButtons)
        {
            takeButtons.SetActive(false);
            ShowDialog(
                buttons.ToString(),
                delegate
                {
                    isBusy = false;
                    KeysControl.IsBlocks = false;
                    GetInputButtons(endTakeButtons);
                },
                delegate
                {
                    isBusy = false;
                    KeysControl.IsBlocks = false;
                    endTakeButtons?.Invoke(ButtonCodes.Zero());
                    Window.Close();
                    basic.SetActive(false);
                },
                delegate
                {
                    isBusy = false;
                    KeysControl.IsBlocks = false;
                    endTakeButtons?.Invoke(buttons);
                    Window.Close();
                    basic.SetActive(false);
                }
            );
        }

        private void OnReceiveInput(AxisCode axis, Action<AxisCode> endTakeAxis)
        {
            takeAxles.SetActive(false);
            ShowDialog(
                axis.ToString(),
                delegate
                {
                    isBusy = false;
                    KeysControl.IsBlocks = false;
                    GetInputAxis(endTakeAxis);
                },
                delegate
                {
                    isBusy = false;
                    KeysControl.IsBlocks = false;
                    endTakeAxis?.Invoke(AxisCode.Zero());
                    Window.Close();
                    basic.SetActive(false);
                },
                delegate
                {
                    isBusy = false;
                    KeysControl.IsBlocks = false;
                    endTakeAxis?.Invoke(axis);
                    Window.Close();
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
