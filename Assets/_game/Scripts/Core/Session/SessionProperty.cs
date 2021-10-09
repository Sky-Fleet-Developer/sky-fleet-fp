using Core.Boot_strapper;
using Core.Explorer.Content;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Session
{
    [DontDestroyOnLoad]
    public class SessionProperty : Singleton<SessionProperty>
    {
        private bool isEndInitSessionProperty = false;

        private LinkedList<Mod> mods;


        public bool IsEndInitSessionProperty()
        {
            return isEndInitSessionProperty;
        }

        public LinkedList<Mod> GetModsSession()
        {
            return mods;
        }

        public void SetModsForSession(LinkedList<Mod> modsSet)
        {
            if(SceneManager.sceneCountInBuildSettings == (byte)SelectorScenes.TypeScene.Menu)
            {
                mods = modsSet;
            }
        }

        public void BeginInitSessionProperty()
        {
            ClearSessionProperty();
        }

        public void EndInitSessionProperty()
        {
            isEndInitSessionProperty = true;
        }
        
        public void ClearSessionProperty()
        {
            isEndInitSessionProperty = false;
            mods = null;
        }
    }
}