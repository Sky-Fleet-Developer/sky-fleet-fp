using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;


namespace System.Threading.Tasks
{
    public static class ButonExtension
    {
        public static ButtonAwiter GetAwaiter(this Button button)
        {
            return new ButtonAwiter(button);
        }

        public static async Task Await(this Button button)
        {
            await button;
        }
    }

    public class ButtonAwiter : INotifyCompletion
    {
        private static Dictionary<Button, ButtonAwiter> awaiters;

        
        private System.Action _continuation;
        private Button button;
        public bool IsCompleted => false;

        public string GetResult() => null;

        public ButtonAwiter(Button button)
        {
            if (awaiters == null) awaiters = new Dictionary<Button, ButtonAwiter>();
            this.button = button;
            _continuation = null;
            button.onClick.AddListener(OnClick);
            awaiters.Add(button, this);
        }

        public void OnCompleted(Action continuation)
        {
            _continuation = continuation;
        }

        private void OnClick()
        {
            button.onClick.RemoveListener(OnClick);
            _continuation?.Invoke();
            awaiters.Remove(button);
        }

        public static void Dispose(Button button)
        {
            if(awaiters.ContainsKey(button) == false) return;
            awaiters[button].Dispose();
            awaiters.Remove(button);
        }
        
        public void Dispose()
        {
            button.onClick.RemoveListener(OnClick);
        }
    }
}