using Core.Boot_strapper;
using Core.Utilities;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.SessionManager.GameProcess;
using static Core.Structure.StructureUpdateModule;
using Sirenix.OdinInspector;

namespace Core.GameSetting
{
    [DontDestroyOnLoad]
    public class InputControl : Singleton<InputControl>, ILoadAtStart
    {
        public event Action OnStartTakeInput;

        public event Action OnEndTakeInput;

        public bool IsTakeInput { get => busyCoroutine != null; }

        private Coroutine busyCoroutine;

        public Task LoadStart()
        {
            return Task.CompletedTask;
        }

        public T GetInput<T>(string categoryName, string inputName) where T : ElementControlSetting
        {
            ControlSetting control = SettingManager.Instance.GetControlSetting();
            ControlSetting.CategoryInputs category = control.Categoryes.Where(x => { return x.Name == categoryName; }).FirstOrDefault();
            if (category != null)
            {
                return category.FindElement<T>(inputName);
            }
            return null;
        }


        public float GetButton(InputButtons buttons)
        {
            for(int i = 0; i < buttons.Keys.Count; i++)
            {
                bool isPress = true;

                for(int i2 = 0; i2 < buttons.Keys[i].KeyCodes.Length - 1; i2++)
                {
                    if(!Input.GetKey(buttons.Keys[i].KeyCodes[i2]))
                    {
                        isPress = false;
                        break;
                    }
                }
                if(isPress && Input.GetKey(buttons.Keys[i].KeyCodes[buttons.Keys[i].KeyCodes.Length-1]))
                {
                    return 1;
                }
            }
            return 0;
        }

        public float GetButtonDown(InputButtons buttons)
        {
            for (int i = 0; i < buttons.Keys.Count; i++)
            {
                bool isPress = true;

                for (int i2 = 0; i2 < buttons.Keys[i].KeyCodes.Length - 1; i2++)
                {
                    if (!Input.GetKey(buttons.Keys[i].KeyCodes[i2]))
                    {
                        isPress = false;
                        break;
                    }
                }
                if (isPress && Input.GetKeyDown(buttons.Keys[i].KeyCodes[buttons.Keys[i].KeyCodes.Length - 1]))
                {
                    return 1;
                }
            }
            return 0;
        }

        public float GetButtonUp(InputButtons buttons)
        {
            for (int i = 0; i < buttons.Keys.Count; i++)
            {
                bool isPress = true;

                for (int i2 = 0; i2 < buttons.Keys[i].KeyCodes.Length - 1; i2++)
                {
                    if (!Input.GetKey(buttons.Keys[i].KeyCodes[i2]))
                    {
                        isPress = false;
                        break;
                    }
                }
                if (isPress && Input.GetKeyUp(buttons.Keys[i].KeyCodes[buttons.Keys[i].KeyCodes.Length - 1]))
                {
                    return 1;
                }
            }
            return 0;
        }


        [System.Serializable]
        public class CorrectInputAxis
        {
            public AxisCode Axis;

            private float oldValue;

            private float sum;

            private float absolute;

            private Vector2 limitSum = new Vector2(-1, 1);

            [Button]
            public void SetAxis(AxisCode axis)
            {
                Axis = axis;
                oldValue = 0;
                sum = 0;
                absolute = 0;
            }

            private void GetVal()
            {
                float val = Input.GetAxisRaw(Axis.Name) * Axis.Multiply;
                if (Axis.Inverse)
                {
                    val *= -1;
                }
                if (Axis.IsAbsolute)
                {
                    sum += val;
                    absolute = val;
                }
                else
                {
                    absolute = val - oldValue;
                    sum = val;
                }
                sum = Mathf.Clamp(sum, limitSum.x, limitSum.y);
                oldValue = val;
            }

            public bool IsNone()
            {
                return string.IsNullOrEmpty(Axis.Name);
            }

            public bool IsAbsolute()
            {
                return Axis.IsAbsolute;
            }

            public float GetInputSum()
            {
                GetVal();
                return sum;
            }

            public float GetInputAbsolute()
            {
                GetVal();
                return absolute;
            }
        }

        #region TakeInput
        public void TakeInputButton(Action<ButtonCodes> endTakeButtons)
        {
            if (busyCoroutine != null)
            {
                throw new MethodAccessException();
            }
            OnStartTakeInput?.Invoke();
            busyCoroutine = StartCoroutine(GetInputButton(endTakeButtons));
        }

        public void TakeInputAxis(Action<AxisCode> endTakeAxis)
        {
            if (busyCoroutine != null)
            {
                throw new MethodAccessException();
            }
            OnStartTakeInput?.Invoke();
            busyCoroutine = StartCoroutine(GetInputAxis(endTakeAxis));
        }

        private IEnumerator GetInputButton(Action<ButtonCodes> endTakeButtons)
        {
            List<KeyCode> keys = new List<KeyCode>();
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
                        }
                    }
                }
                if (keys.Count > 0 && Input.GetKeyUp(keys[0]))
                {
                    break;
                }
                yield return null;
            }

            busyCoroutine = null;
            endTakeButtons(new ButtonCodes(keys.ToArray()));
            OnEndTakeInput?.Invoke();
        }

        private IEnumerator GetInputAxis(Action<AxisCode> endTakeAxis)
        {
            List<string> mouseAxles = new List<string>() { "Mouse X", "Mouse Y", "Mouse ScrollWheel" };
            List<float> mouseAxlesCorrectMultiply = new List<float>() { 10, Screen.width / Screen.height * 10, 200 };
            List<string> joyAxles = new List<string>() { "Joy axe 1", "Joy axe 2", "Joy axe 3", "Joy axe 4", "Joy axe 5", "Joy axe 6", "Joy axe 7", "Joy axe 8" };

            AxisCode axis = new AxisCode();
            List<float> oldValue = new List<float>(11);
            oldValue.Add(Input.GetAxisRaw(mouseAxles[0]));
            oldValue.Add(Input.GetAxisRaw(mouseAxles[1]));
            oldValue.Add(Input.GetAxisRaw(mouseAxles[2]));
            for (int i = 0; i < joyAxles.Count; i++)
            {
                oldValue.Add(Input.GetAxisRaw(joyAxles[i]));
            }
            while (true)
            {
                bool isEnd = false;
                for (int i = 0; i < mouseAxles.Count; i++)
                {
                    float curValue = Mathf.Abs(Input.GetAxisRaw(mouseAxles[i])) * mouseAxlesCorrectMultiply[i];
                    if (curValue + oldValue[i] > 2f)
                    {
                        axis.Name = mouseAxles[i];
                        isEnd = true;
                        break;
                    }
                    oldValue[i] = curValue;
                }

                if (Input.GetJoystickNames().Length != 0)
                {
                    for (int i = 0; i < joyAxles.Count; i++)
                    {
                        if (Mathf.Abs(Input.GetAxisRaw(joyAxles[i])) > 0.4f)
                        {
                            axis.Name = joyAxles[i];
                            isEnd = true;
                            break;
                        }
                    }
                }

                if (isEnd)
                {
                    break;
                }

                yield return null;
            }
            busyCoroutine = null;
            endTakeAxis?.Invoke(axis);
            OnEndTakeInput?.Invoke();
        }

    }
    #endregion
}