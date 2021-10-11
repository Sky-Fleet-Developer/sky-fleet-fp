using System;
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
using Core.Utilities;
using Newtonsoft.Json;
using Sirenix.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Serializer = Core.ContentSerializer.Serializer;

namespace Core.SessionManager.SaveService
{
    [System.Serializable]
    public class SaveLoad
    {
        public class LoadSettingSession : SessionSettings
        {
            public LinkedList<string> NoHaveMods; 
        }

        public void Save()
        {
            Debug.Log("Begin to save the session...");

            IEnumerable<IStructure> structures = CollectStructures();

            Serializer serializer = StructureProvider.GetSerializer();

            List<StructureBundle> bundles = serializer.GetBundlesFor(structures);

            var state = new State(bundles);

            SaveToFile(state);
            Debug.Log("Session was saved successfully!");
        }

        public LoadSettingSession LoadBaseInfoSession(string path)
        {
            LoadSettingSession sessionSettings = new LoadSettingSession();
            sessionSettings.NoHaveMods = new LinkedList<string>();
            sessionSettings.mods = new LinkedList<Mod>();

            FileStream file = File.Open(path, FileMode.Open);

            BaseInfoSession(file, sessionSettings);

            file.Close();
            return sessionSettings;
        }

        private LoadSettingSession BaseInfoSession(FileStream file, LoadSettingSession sessionSettings)
        {
            List<Mod> mods = ModReader.Instance.GetMods();
            if (mods == null)
            {
                ModReader.Instance.Load().Wait();
            }
            mods = ModReader.Instance.GetMods();

            byte[] intBuf = new byte[sizeof(int)];
            int count = 0;

            file.Read(intBuf, 0, sizeof(int));
            count = BitConverter.ToInt32(intBuf, 0);
            byte[] nameSave = new byte[count];
            file.Read(nameSave, 0, count);
            sessionSettings.name = Encoding.ASCII.GetString(nameSave);

            file.Read(intBuf, 0, sizeof(int));
            int countMods = BitConverter.ToInt32(intBuf, 0);

            for (int i = 0; i < countMods; i++)
            {
                file.Read(intBuf, 0, sizeof(int));
                count = BitConverter.ToInt32(intBuf, 0);
                byte[] nameModB = new byte[count];
                file.Read(nameModB, 0, count);
                string nameMod = Encoding.ASCII.GetString(nameModB);
                Mod mod = mods.Where(x => { return x.name == nameMod; }).FirstOrDefault();
                if (mod == null || mod == default)
                {
                    sessionSettings.NoHaveMods.AddLast(nameMod);
                }
                else
                {
                    sessionSettings.mods.AddLast(mod);
                }
            }
            return sessionSettings;
        }

        public async Task Load(string fileName)
        {
            State state = LoadAtPath(fileName);

            //TODO: подождать пока загрузится сцена меню, если мы ещё не в ней

            List<System.Reflection.Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            LinkedList<Mod> mods = Session.Instance.Settings.mods;
            if (mods != null)
            {
                foreach (Mod mod in mods)
                {
                    assemblies.Add(mod.assembly);
                }
            }

            Deserializer deserializer = StructureProvider.GetDeserializer(assemblies.ToArray());

            List<Task> awaiters = new List<Task>();
            foreach (StructureBundle structureBundle in state.structuresCache)
            {
                Task<IStructure> task = structureBundle.ConstructStructure(deserializer);
                awaiters.Add(task);
            }

            await Task.WhenAll(awaiters);
        }

        private State LoadAtPath(string fileName)
        {
            DirectoryInfo infoPath = Directory.GetParent(Application.dataPath);
            string dataExportPath = $"{infoPath.FullName}/{PathStorage.BASE_DATA_PATH}";
            string path = $"{dataExportPath}/{PathStorage.DATA_SESSION_PRESETS}/";
            string json = File.ReadAllText($"{path}{fileName}");
            try
            {
                State state = JsonConvert.DeserializeObject<State>(json);

                // TODO: если Application.isPlaying, загрузить список модов из ModLoader

                return state;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void SaveToFile(State state)
        {

            SessionSettings sessionSettings = Session.Instance.Settings;


            string saveName = sessionSettings.name;
            if (string.IsNullOrEmpty(saveName)) saveName = $"Unknown session {Random.Range(0, 1000):0000}";
            MemoryStream memoryBaseInfo = new MemoryStream();

            byte[] nameB = Encoding.ASCII.GetBytes(saveName);
            memoryBaseInfo.Write(BitConverter.GetBytes(nameB.Length), 0, sizeof(int));
            memoryBaseInfo.Write(nameB, 0, nameB.Length);
            memoryBaseInfo.Write(BitConverter.GetBytes(sessionSettings.mods.Count), 0, sizeof(int));
            foreach (Mod mod in sessionSettings.mods)
            {
                nameB = Encoding.ASCII.GetBytes(mod.name);
                memoryBaseInfo.Write(BitConverter.GetBytes(nameB.Length), 0, sizeof(int));
                memoryBaseInfo.Write(nameB, 0, nameB.Length);
            }

            string json = JsonConvert.SerializeObject(state);
            byte[] jsonBytes = Encoding.ASCII.GetBytes(json);
            memoryBaseInfo.Write(BitConverter.GetBytes(jsonBytes.Length), 0, sizeof(int));
            memoryBaseInfo.Write(jsonBytes, 0, jsonBytes.Length);

            DirectoryInfo infoPath = Directory.GetParent(Application.dataPath);
            string dataExportPath = $"{infoPath.FullName}/{PathStorage.BASE_DATA_PATH}";
            string path = $"{dataExportPath}/{PathStorage.DATA_SESSION_PRESETS}/";
            FileStream file = File.Open($"{path}{saveName}." + PathStorage.SESSION_TYPE_FILE, FileMode.OpenOrCreate);
            memoryBaseInfo.Seek(0, SeekOrigin.Begin);
            memoryBaseInfo.CopyTo(file);
            file.Close();
        }

        private IEnumerable<IStructure> CollectStructures()
        {
            return Application.isPlaying ? CollectInRuntime() : CollectInEditor();
        }

        private IEnumerable<IStructure> CollectInRuntime()
        {
            return StructureUpdateModule.Structures.Clone();
        }

        private IEnumerable<IStructure> CollectInEditor()
        {
            List<IStructure> result = new List<IStructure>();

            foreach (var monobeh in Object.FindObjectsOfType<MonoBehaviour>())
            {
                if (monobeh is IStructure structure) result.Add(structure);
            }

            return result;
        }
    }
}
