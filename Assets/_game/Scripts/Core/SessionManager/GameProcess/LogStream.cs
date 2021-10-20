using Core.Utilities;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Core.Boot_strapper;
using System.Threading.Tasks;
using System.Text;

namespace Core.SessionManager.GameProcess
{
    [DontDestroyOnLoad]
    public class LogStream : Singleton<LogStream>, ILoadAtStart
    {
        public event Action<LogString> PushLogCall;

        [SerializeField] private LinkedList<LogString> logs = new LinkedList<LogString>();

        public const int MaxCountLogs = 20;

        private FileStream fileLog;

        public Task Load()
        {
            string pathBase = PathStorage.GetPathToLogs();
            if (!Directory.Exists(pathBase))
            {
                Directory.CreateDirectory(pathBase);
            }
            else
            {
                Directory.Delete(pathBase, true);
                Directory.CreateDirectory(pathBase);
            }
            DateTime time = DateTime.Now;
            string nameSave = time.Year + "-" + time.Month + "-" + time.Day + " - " + time.Hour + "." + time.Minute;
            string path = pathBase + "\\" + nameSave + ".txt";
            fileLog = File.Open(path, FileMode.OpenOrCreate);
            return Task.CompletedTask;
        }

        public void PushLog(string text)
        {
            FlushLog(text);

            LogString log = new LogString();
            log.Log = text;
            logs.AddLast(log);
            PushLogCall?.Invoke(log);
            if(CheckIsOverflow())
            {
                RemoveBackLog();
            }
        }
        
        private bool CheckIsOverflow()
        {
            if(logs.Count > MaxCountLogs)
            {
                return true;
            }
            return false;
        }

        private void RemoveBackLog()
        {
            LinkedListNode<LogString> log = logs.Last;
            log.Value.Unload();
            logs.RemoveLast();
        }

        private void FlushLog(string log)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(DateTime.Now.ToString() + "-: " + log + "\n");
            fileLog.Write(info, 0, info.Length);
            fileLog.Flush();
        }

        private void OnApplicationQuit()
        {
            if (fileLog != null)
            {
                fileLog.Close();
                fileLog = null;
            }
        }

        private void OnDestroy()
        {
            if(fileLog != null)
            {
                fileLog.Close();
                fileLog = null;
            }
        }
    }
}