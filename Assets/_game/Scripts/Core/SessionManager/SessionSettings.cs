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
        public string serializationVersion = "0.0.1";
        public List<string> modsNames;
        public List<string> missingMods = new List<string>();
        public LinkedList<Mod> mods = new LinkedList<Mod>();

        public void Clear()
        {
            mods = null;
        }
    }

}
