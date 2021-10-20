using System;
using System.IO;


namespace Core.SessionManager.SaveService
{

    public class SaveLoadUtility
    {
        private SaveLoad saveLoad = new SaveLoad();

        public void SaveWithName(string name)
        {
            string pathBase = PathStorage.GetPathToSessionSave();
            DirectoryInfo info;
            if (!Directory.Exists(pathBase + "\\" + name))
            {
                info = Directory.CreateDirectory(pathBase + "\\" + name);
            }
            else
            {
                info = new DirectoryInfo(pathBase + "\\" + name);
            }

            DateTime time = DateTime.Now;
            string nameSave = time.Year + "-" + time.Month + "-" + time.Day + " - " + time.Hour + "." + time.Minute;
            string path = info.FullName + "\\" + nameSave + "." + PathStorage.SESSION_TYPE_FILE;
            saveLoad.Save(path, nameSave);
        }

        public void SaveSession(string path, string name)
        {
            path = path + "\\" + name + "." + PathStorage.SESSION_TYPE_FILE;
            saveLoad.Save(path, name);
        }

        public string CreateDirectorySession(string name)
        {
            string pathBase = PathStorage.GetPathToSessionSave();
            string retName = name;
            if (Directory.Exists(pathBase + "\\" + name))
            {
                string[] listD = GetDirectoryWithName(name, pathBase);
                if (listD.Length == 0)
                {
                    retName = name + "_1";
                }
                else
                {
                    retName = name + "_" + (listD.Length + 1);
                }
            }
            Directory.CreateDirectory(pathBase + "\\" + retName);
            return retName;
        }

        public bool CheckIsCanSave(string name, string path)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            else
            {
                return Directory.Exists(path);
            }
        }

        private string[] GetDirectoryWithName(string name, string path)
        {
            string[] directores = Directory.GetDirectories(path, name + "_*");
            return directores;
        }

    }
}