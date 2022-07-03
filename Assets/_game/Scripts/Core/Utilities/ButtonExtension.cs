using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace Core.Utilities
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
        private static Dictionary<Button, ButtonAwiter> waiting;

        
        private System.Action _continuation;
        private Button button;
        public bool IsCompleted => false;

        public string GetResult() => null;

        public ButtonAwiter(Button button)
        {
            if (waiting == null) waiting = new Dictionary<Button, ButtonAwiter>();
            this.button = button;
            _continuation = null;
            button.onClick.AddListener(OnClick);
            waiting.Add(button, this);
        }

        public void OnCompleted(Action continuation)
        {
            _continuation = continuation;
        }

        private void OnClick()
        {
            button.onClick.RemoveListener(OnClick);
            _continuation?.Invoke();
            waiting.Remove(button);
        }

        public static void Dispose(Button button)
        {
            if(waiting.ContainsKey(button) == false) return;
            waiting[button].Dispose();
            waiting.Remove(button);
        }
        
        public void Dispose()
        {
            button.onClick.RemoveListener(OnClick);
        }
    }
}