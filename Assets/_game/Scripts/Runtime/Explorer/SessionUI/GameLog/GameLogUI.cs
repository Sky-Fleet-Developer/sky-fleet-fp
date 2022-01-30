using Core.SessionManager.GameProcess;
using Core.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.SessionUI.GameLog
{
    public class GameLogUI : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private Text prefabItem;

        [Header("List items"), SerializeField] private GameObject[] items;

        private float time = 0;
        private bool lockTime = false;

        private const float DeltaTime = 0.1f;

        private bool isHide = false;

        private void Start()
        {
            LogStream.PushLogCall += OnPushLog;
            PauseGame.Instance.OnPause += SetOnPause;
            PauseGame.Instance.OnResume += OnSetUnpause;
            SetActiveUIElements(false);
        }

        private void LateUpdate()
        {
            if (time > 0)
            {
                if (!lockTime)
                {
                    time -= DeltaTime;
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
            PauseGame.Instance.OnPause -= SetOnPause;
            PauseGame.Instance.OnResume -= OnSetUnpause;
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

        private void SetOnPause()
        {
            lockTime = true;
            time = DeltaTime;
        }

        private void OnSetUnpause()
        {
            lockTime = false;
        }
    }
}