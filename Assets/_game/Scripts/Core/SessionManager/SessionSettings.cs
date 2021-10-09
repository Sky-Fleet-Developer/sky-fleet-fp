using System.Collections.Generic;
using Core.Explorer.Content;
using UnityEngine;

namespace Core.SessionManager
{
    [System.Serializable]
    public struct SessionSettings
    {
        public string name;
        public LinkedList<Mod> mods;

        public void Clear()
        {
            mods = null;
        }
    }
}
