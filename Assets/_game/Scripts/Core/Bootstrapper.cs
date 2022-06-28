using Core.Boot_strapper;
using Core.SessionManager.GameProcess;
using Core.Utilities;
using UnityEngine;

namespace Core
{
    public class Bootstrapper : MonoBehaviour
    {
        public static LateEvent OnLoadComplete = new LateEvent();
        private async void Start()
        {
            foreach (ILoadAtStart load in GetComponentsInChildren<ILoadAtStart>(true))
            {
                if(load.enabled) await load.Load();
            }
            OnLoadComplete.Invoke();
            OnLoadComplete = new LateEvent();
        }
    }
}