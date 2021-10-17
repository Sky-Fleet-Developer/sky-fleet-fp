using Core.Boot_strapper;
using Core.SessionManager.SaveService;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.ContentSerializer;
using Core.SessionManager.GameProcess;

namespace Core.SessionManager
{
    public class FastSave : Singleton<FastSave>, ILoadAtStart
    {
        private SaveLoadUtility saver;
        public Task Load()
        {
            saver = new SaveLoadUtility();
            HotKeys.Instance.FastSave += Save;
            return Task.CompletedTask;
        }

        private void Save()
        {
            if(saver.CheckIsCanSave(Session.Instance.Settings.name, PathStorage.GetPathToSessionSave()))
            {
                saver.SaveWithName(Session.Instance.Settings.name);
                LogStream.Instance.PushLog("Session saved");
            }
            else
            {
                LogStream.Instance.PushLog("Session could not be saved");
            }
        }
    }
}