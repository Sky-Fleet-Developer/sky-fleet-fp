using System.Collections.Generic;
using Core.Explorer.Content;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.SessionManager
{
    [System.Serializable]
    public class SessionSettings
    {
        public string name;
        public LinkedList<Mod> mods = new LinkedList<Mod>();

        public void Clear()
        {
            mods = null;
        }
    }

}
