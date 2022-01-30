using Core.Boot_strapper;
using Core.SessionManager.SaveService;
using Core.Utilities;
using System.Threading.Tasks;
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
                LogStream.PushLog("Session saved");
            }
            else
            {
                LogStream.PushLog("Session could not be saved");
            }
        }
    }
}