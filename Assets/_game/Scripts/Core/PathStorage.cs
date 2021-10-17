using System.IO;
using UnityEngine;

namespace Core
{
    public class PathStorage
    {
        public static readonly string BASE_MODS_PATH = "/Mod/Export";
        public static readonly string BASE_PATH_TEXTURES = "/Mod/Export/Textures";
        public static readonly string BASE_PATH_MODELS = "/Mod/Export/Models";
        public static readonly string MOD_RELETIVE_PATH_TEXTURES = "/Textures";
        public static readonly string MOD_RELETIVE_PATH_MODELS = "/Models";
        public static readonly string BASE_MOD_FILE_DEFINE = "modDefine.json";
        public static readonly string ASSEMBLY_FILE_DEFINE = "ModAssembly.dll";
        
        
        public static readonly string BASE_DATA_PATH = "Data";
        public static readonly string BASE_LOGS_PATH = "CustomLogs";
        public static readonly string DATA_SESSION_PRESETS = "SessionPresets";
        public static readonly string DATA_SESSION_SAVE = "SessionSave";
        public static readonly string SESSION_TYPE_FILE = "save";


        public static string GetPathToSessionSave()
        {
            string pathU = Application.dataPath;
            DirectoryInfo infoPath = Directory.GetParent(pathU);
            pathU = infoPath.FullName + "\\" + PathStorage.BASE_DATA_PATH + "\\" + PathStorage.DATA_SESSION_SAVE;
            return pathU;
        }


        public static string GetPathToSessionPresets()
        {
            string pathU = Application.dataPath;
            DirectoryInfo infoPath = Directory.GetParent(pathU);
            pathU = infoPath.FullName + "\\" + PathStorage.BASE_DATA_PATH + "\\" + PathStorage.DATA_SESSION_PRESETS;
            return pathU;
        }

        public static string GetPathToLogs()
        {
            string pathU = Application.dataPath;
            DirectoryInfo infoPath = Directory.GetParent(pathU);
            pathU = infoPath.FullName + "\\" + PathStorage.BASE_LOGS_PATH;
            return pathU;
        }
    }
}
