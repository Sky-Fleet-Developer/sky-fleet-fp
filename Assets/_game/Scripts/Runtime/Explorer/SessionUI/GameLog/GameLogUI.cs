using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core.SessionManager.GameProcess;
using Core.Utilities.UI;
using Core.Utilities;

namespace Runtime.Explorer.SessionUI
{
    public class GameLogUI : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private Text prefabItem;

        [Header("List items"), SerializeField] private GameObject[] items;

        private float time = 0;
        private bool lockTime = false;

        private const float deltaTime = 0.1f;

        private bool isHide = false;

        private void Start()
        {
            LogStream.Instance.PushLogCall += OnPushLog;
            PauseGame.Instance.PauseOn += OnSetPause;
            PauseGame.Instance.PauseOff += OnSetUnpause;
            SetActiveUIElements(false);
        }

        private void LateUpdate()
        {
            if (time > 0)
            {
                if (!lockTime)
                {
                    time -= deltaTime;
                }
                if(time <= 0 && !isHide)
                {
                    SetActiveUIElements(false);
                    isHide = true;
                }
                else if(isHide)
                {
                    SetActiveUIElements(true);
                    isHide = false;
                }
            }
        }

        private void OnDestroy()
        {
            PauseGame.Instance.PauseOn -= OnSetPause;
            PauseGame.Instance.PauseOff -= OnSetUnpause;
        }

        private void OnPushLog(LogString log)
        {
            time = 50;
            Text item = DynamicPool.Instance.Get(prefabItem, content);
            item.text = log.Log;
            log.UnLoadLog += delegate { DynamicPool.Instance.Return(item); };
        }

        private void SetActiveUIElements(bool isActive)
        {
            foreach(GameObject i in items)
            {
                i.SetActive(isActive);
            }
        }

        private void OnSetPause()
        {
            lockTime = true;
            time = deltaTime;
        }

        private void OnSetUnpause()
        {
            lockTime = false;
        }
    }
}