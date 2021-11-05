using System;
using Core.Utilities;
using Core.GameSetting;
using UnityEngine;
using Core.Boot_strapper;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Core
{

    public class KeysControl : Singleton<KeysControl>, ILoadAtStart
    {
        public HotKeys Hot { get; private set; }

        public static bool IsBlocks { get; set; }

        private List<Request> requests;


        public Task LoadStart()
        {
            Hot = new HotKeys();
            requests = new List<Request>();
            return Task.CompletedTask;
        }

        public Request RegisterRequest(string category, string nameButton, PressType type, Action action)
        {
            InputButtons button = InputControl.Instance.GetInput<InputButtons>(category, nameButton);
            if (button == null)
            {
                return null;
            }
            Request request = new Request();
            request.Button = button;
            request.PressMod = type;
            request.CallPress = x => { action?.Invoke(); };
            requests.Add(request);
            return request;
        }

        public Request RegisterRequest(string category, string nameButton, PressType type, Action<PressType> action)
        {
            InputButtons button = InputControl.Instance.GetInput<InputButtons>(category, nameButton);
            if (button == null)
            {
                return null;
            }
            Request request = new Request();
            request.Button = button;
            request.PressMod = type;
            request.CallPress = action;
            requests.Add(request);
            return request;
        }

        public Request RegisterRequest(InputButtons button, PressType type, Action action)
        {
            Request request = new Request();
            request.Button = button;
            request.PressMod = type;
            request.CallPress = x => { action?.Invoke(); };
            requests.Add(request);
            return request;
        }

        public Request RegisterRequest(InputButtons button, PressType type, Action<PressType> action)
        {
            Request request = new Request();
            request.Button = button;
            request.PressMod = type;
            request.CallPress = action;
            requests.Add(request);
            return request;
        }

        public void RemoveReques(Request request)
        {
            requests.Remove(request);
        }

        private void Update()
        {
            if (!IsBlocks)
            {
                Hot.Update();

                    for (int i = 0; i < requests.Count; i++)
                    {
                        if (requests[i].PressMod == PressType.Up && InputControl.Instance.GetButtonUp(requests[i].Button) > 0)
                        {
                            requests[i].CallPress?.Invoke(PressType.Up);
                        }
                        if (requests[i].PressMod == PressType.Always && InputControl.Instance.GetButton(requests[i].Button) > 0)
                        {
                        requests[i].CallPress?.Invoke(PressType.Always);
                        }
                        if (requests[i].PressMod == PressType.Down && InputControl.Instance.GetButtonDown(requests[i].Button) > 0)
                        {
                        requests[i].CallPress?.Invoke(PressType.Down);
                        }
                    }
            }
        }



        public enum PressType
        {
            Down = 1,
            Up = 2,
            Always = 4,
        }

        public class Request
        {
            public InputButtons Button;
            public PressType PressMod;

            public Action<PressType> CallPress;
        }
    }
}