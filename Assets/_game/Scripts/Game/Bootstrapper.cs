using System.Collections;
using System.Collections.Generic;
using Management;
using Structure;
using UnityEngine;

namespace Game
{
    public class Bootstrapper : MonoBehaviour
    {
        void Awake()
        {
            StructureManager.CheckInstance();
            GameData.CheckInstance();
            GameData.Instance.OnEnable();
        }

    }
}
