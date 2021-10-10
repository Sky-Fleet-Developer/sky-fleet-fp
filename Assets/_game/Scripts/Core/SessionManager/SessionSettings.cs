using System.Collections.Generic;
using Core.Explorer.Content;
using UnityEngine;

namespace Core.SessionManager
{
    [System.Serializable]
    public class SessionSettings
    {
        public string name;
        public LinkedList<Mod> mods;

        public void Clear()
        {
            mods = null;
        }
    }
}
