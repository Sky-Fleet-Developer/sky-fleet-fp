using Core.Boot_strapper;
using Core.SessionManager.GameProcess;
using UnityEngine;

namespace Core
{
    public class Bootstrapper : MonoBehaviour
    {
        private async void Start()
        {
            foreach (ILoadAtStart load in GetComponentsInChildren<ILoadAtStart>())
            {
                await load.LoadStart();
            }
        }
    }
}