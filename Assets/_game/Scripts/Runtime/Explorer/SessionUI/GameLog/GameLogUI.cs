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

        private void Start()
        {
            LogStream.Instance.PushLogCall += OnPushLog;
        }

        private void OnPushLog(LogString log)
        {

            Text item = DynamicPool.Instance.Get(prefabItem, content);
            item.text = log.Log;
            log.UnLoadLog += delegate { DynamicPool.Instance.Return(item); };
        }
    }
}