using Core.Boot_strapper;
using Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.SessionManager.GameProcess;
using static Core.Structure.StructureUpdateModule;

namespace Core.GameSetting
{
    [DontDestroyOnLoad]
    public class InputControl : Singleton<InputControl>, ILoadAtStart
    {
        public event Action OnStartTakeInput;

        public event Action<InputAbstractType> OnEndTakeInput;

        private Coroutine busyCoroutine;



        public Task Load()
        {
            return Task.CompletedTask;
        }


        public void TakeInput()
        {
            if (busyCoroutine != null)
                return;
            OnStartTakeInput?.Invoke();
            PauseGame.Instance.SetOffPause();
            busyCoroutine = StartCoroutine(GetInput());
        }

        private IEnumerator GetInput()
        {
            List<KeyCode> keys = new List<KeyCode>();
            float endTime = -1;
            while (true)
            {
                if (Input.anyKey)
                {
                    foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                    {
                        if (Input.GetKeyDown(key))
                        {
                            bool isAllPress = true;
                            if (keys.Count > 0 && !Input.GetKey(keys[0]))
                            {
                                isAllPress = false;
                            }


                            if (isAllPress)
                            {
                                keys.Add(key);
                            }

                            endTime = 0.8f;
                        }
                    }

                }
                if (endTime != -1)
                {
                    endTime -= Time.deltaTime;
                    if (endTime < 0)
                    {
                        break;
                    }
                }
                yield return null;
            }

            foreach (KeyCode keySave in keys)
            {
                Debug.Log(keySave);
            }

            busyCoroutine = null;
        }

    }
}