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

        public static readonly string SETTING_DATA_PATH = "Setting";
        public static readonly string SETTING_DATA_FILE = "Setting.opt";

        public static readonly string LANDSCAPES_DATA_PATH = "Landscapes";
        
        public static string GetPathToSessionSave()
        {
            DirectoryInfo infoPath = Directory.GetParent(Application.dataPath);
            string pathU = infoPath.FullName + "\\" + PathStorage.BASE_DATA_PATH + "\\" + PathStorage.DATA_SESSION_SAVE;
            return pathU;
        }


        public static string GetPathToSessionPresets()
        {
            DirectoryInfo infoPath = Directory.GetParent(Application.dataPath);
            string pathU = infoPath.FullName + "\\" + PathStorage.BASE_DATA_PATH + "\\" + PathStorage.DATA_SESSION_PRESETS;
            return pathU;
        }

        public static string GetPathToSettingFile()
        {
            DirectoryInfo infoPath = Directory.GetParent(Application.dataPath);
            string pathU = infoPath.FullName + "\\" + PathStorage.SETTING_DATA_PATH + "\\" + PathStorage.SETTING_DATA_FILE;
            return pathU;
        }

        public static string GetPathToSettingDirectory()
        {
            DirectoryInfo infoPath = Directory.GetParent(Application.dataPath);
            string pathU = infoPath.FullName + "\\" + PathStorage.SETTING_DATA_PATH;
            return pathU;
        }

        public static string GetPathToLandscapesDirectory()
        {
            DirectoryInfo infoPath = Directory.GetParent(Application.dataPath);
            string pathU = infoPath.FullName + "\\" + PathStorage.LANDSCAPES_DATA_PATH;
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
