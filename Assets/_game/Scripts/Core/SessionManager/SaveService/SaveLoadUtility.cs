using System;
using System.Runtime;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Core.ContentSerializer.Bundles;
using Core.ContentSerializer.Providers;
using Core.Explorer.Content;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Utilities;
using Newtonsoft.Json;
using Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;


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
            saveLoad.Save(path, name);
        }

        public string CreateDirectorySession(string name)
        {
            string pathBase = PathStorage.GetPathToSessionSave();
            string retName = name;
            if (Directory.Exists(pathBase + "\\" + name))
            {
                string[] listD = GetDirectoryWithName(name, pathBase);
                if(listD.Length == 0)
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

        private string[] GetDirectoryWithName(string name, string path)
        {
            string[] directores = Directory.GetDirectories(path, name + "_*");
            return directores;
        }
    }
}