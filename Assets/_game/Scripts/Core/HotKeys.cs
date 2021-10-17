using System;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Boot_strapper;
using System.Threading.Tasks;

namespace Core
{
    public class HotKeys : Singleton<HotKeys>, ILoadAtStart
    {
        public event Action FastSave;

        public Task Load()
        {
            return Task.CompletedTask;
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F5))
            {
                FastSave?.Invoke();
            }
        }
    }
}