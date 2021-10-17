using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.SessionManager.GameProcess
{
    public class LogString
    {
        public event Action UnLoadLog;
        public string Log;

        public void Unload()
        {
            UnLoadLog?.Invoke();
        }
    }
}