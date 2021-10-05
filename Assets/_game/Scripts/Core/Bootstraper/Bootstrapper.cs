using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Boot_strapper
{
    public class Bootstrapper : MonoBehaviour
    {
        
        private async void Awake()
        {
            foreach(ILoadAtStart load in GetComponentsInChildren<ILoadAtStart>())
            {
                await load.Load();
            }
        }

    }

}