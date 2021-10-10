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
        
        public SessionSettings LoadBaseInfoSession(string path)
        {
            return null;
        }

        public async Task Load(string fileName)
        {
            var state = LoadAtPath(fileName);

            //TODO: подождать пока загрузится сцена меню, если мы ещё не в ней
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var mods = Session.Instance.Settings.mods;
            if (mods != null)
            {
                foreach (var mod in mods)
                {
                    assemblies.Add(mod.assembly);
                }
            }

            Deserializer deserializer = StructureProvider.GetDeserializer(assemblies.ToArray());

            var awaiters = new List<Task>();
            foreach (var structureBundle in state.structuresCache)
            {
                var task = structureBundle.ConstructStructure(deserializer);
                awaiters.Add(task);
            }

            await Task.WhenAll(awaiters);
        }

        private State LoadAtPath(string fileName)
        {
            DirectoryInfo infoPath = Directory.GetParent(Application.dataPath);
            string dataExportPath = $"{infoPath.FullName}/{PathStorage.BASE_DATA_PATH}";
            string path = $"{dataExportPath}/{PathStorage.DATA_SESSION_PRESETS}/";
            var json = File.ReadAllText($"{path}{fileName}");
            try
            {
                var state = JsonConvert.DeserializeObject<State>(json);
                
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
            foreach(Mod mod in sessionSettings.mods)
            {
                nameB = Encoding.ASCII.GetBytes(mod.name);
                memoryBaseInfo.Write(BitConverter.GetBytes(nameB.Length), 0, sizeof(int));
                memoryBaseInfo.Write(nameB, 0, nameB.Length);
            }

            string json = JsonConvert.SerializeObject(state);
            


            DirectoryInfo infoPath = Directory.GetParent(Application.dataPath);
            string dataExportPath = $"{infoPath.FullName}/{PathStorage.BASE_DATA_PATH}";

            string path = $"{dataExportPath}/{PathStorage.DATA_SESSION_PRESETS}/";
            Directory.CreateDirectory(path);
            
            File.WriteAllText($"{path}{saveName}.save", json);
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
                if(monobeh is IStructure structure) result.Add(structure);
            }

            return result;
        }
    }
}
