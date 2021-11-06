using System;
using System.Linq;
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

        private LinkedList<KeyToRequest> clearKeysQueue;

        public Task LoadStart()
        {
            Hot = new HotKeys();
            requests = new List<Request>();
            clearKeysQueue = new LinkedList<KeyToRequest>();
            return Task.CompletedTask;
        }

        public KeyToRequest RegisterRequest(string category, string nameButton, PressType type, Action action)
        {
            InputButtons button = InputControl.Instance.GetInput<InputButtons>(category, nameButton);
            if (button == null)
            {
                return null;
            }
            return RequestCreateOrAdd(button, type, x => { action?.Invoke(); });
        }

        public KeyToRequest RegisterRequest(string category, string nameButton, PressType type, Action<PressType> action)
        {
            InputButtons button = InputControl.Instance.GetInput<InputButtons>(category, nameButton);
            if (button == null)
            {
                return null;
            }
            return RequestCreateOrAdd(button, type, x => { action?.Invoke(x); });
        }

        public KeyToRequest RegisterRequest(InputButtons button, PressType type, Action action)
        {
            return RequestCreateOrAdd(button, type, x => { action?.Invoke(); });
        }

        public KeyToRequest RegisterRequest(InputButtons button, PressType type, Action<PressType> action)
        {
            return RequestCreateOrAdd(button, type, x => { action?.Invoke(x); });
        }

        private KeyToRequest RequestCreateOrAdd(InputButtons button, PressType type, Action<PressType> action)
        {
            Request request = FindRequest(button);
            if (request != null)
            {
                request.Keys.Add(new KeyToRequest() { CallPress = action, PressMod = type });
                return request.Keys[request.Keys.Count - 1];
            }
            else
            {
                KeyToRequest key = CreateNewRequest(button, type);
                key.CallPress = action;
                return key;
            }
        }

        private Request FindRequest(InputButtons button)
        {
            return requests.Where(x => { return x.Button == button; }).FirstOrDefault();
        }

        private KeyToRequest CreateNewRequest(InputButtons button, PressType type)
        {
            Request request = new Request();
            request.Button = button;
            KeyToRequest key = new KeyToRequest();
            key.PressMod = type;
            request.Keys = new List<KeyToRequest>();
            request.Keys.Add(key);

            requests.Add(request);
            return key;
        }

        public void RemoveRequest(KeyToRequest keyToRequest)
        {
            clearKeysQueue.AddLast(keyToRequest);
        }


        private void RemoveKeyFromRequest(KeyToRequest keyToRequest)
        {
            Request req = null;
            requests.ForEach(x =>
            {
                KeyToRequest key = x.Keys.Where(x => { return x == keyToRequest; }).FirstOrDefault();
                if (key != null)
                {
                    req = x;
                    req.Keys.Remove(key);
                }
            });

            if (req != null && req.Keys.Count == 0)
            {
                requests.Remove(req);
            }
        }

        private void Update()
        {
            if (!IsBlocks)
            {
                Hot.Update();

                for (int i = 0; i < requests.Count; i++)
                {
                    if (InputControl.Instance.GetButtonDown(requests[i].Button) > 0)
                    {
                        for (int i2 = 0; i2 < requests[i].Keys.Count; i2++)
                        {
                            if (requests[i].Keys[i2].PressMod == PressType.Down) requests[i].Keys[i2].CallPress(PressType.Down);
                        }
                    }
                    if (InputControl.Instance.GetButtonUp(requests[i].Button) > 0)
                    {
                        for (int i2 = 0; i2 < requests[i].Keys.Count; i2++)
                        {
                            if (requests[i].Keys[i2].PressMod == PressType.Up) requests[i].Keys[i2].CallPress(PressType.Up);
                        }
                    }
                    if (InputControl.Instance.GetButton(requests[i].Button) > 0)
                    {
                        for (int i2 = 0; i2 < requests[i].Keys.Count; i2++)
                        {
                            if (requests[i].Keys[i2].PressMod == PressType.Always) requests[i].Keys[i2].CallPress(PressType.Always);
                        }
                    }
                }

                foreach (KeyToRequest key in clearKeysQueue)
                {
                    RemoveKeyFromRequest(key);
                }
                clearKeysQueue.Clear();
            }
        }

        public enum PressType
        {
            Down = 1,
            Up = 2,
            Always = 4,
        }

        private class Request
        {
            public InputButtons Button;
            public List<KeyToRequest> Keys;
        }

        public class KeyToRequest
        {
            public PressType PressMod;
            public Action<PressType> CallPress;
        }
    }
}