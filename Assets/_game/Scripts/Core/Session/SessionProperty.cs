using Core.Explorer.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Session
{
    public static class SessionProperty
    {
        private static LinkedList<Mod> mods;

        public static LinkedList<Mod> GetModsSession()
        {
            return mods;
        }

        public static void SetModsForSession(LinkedList<Mod> modsSet)
        {
            if(SceneManager.sceneCountInBuildSettings == 0)
            {
                mods = modsSet;
            }
        }
    }
}