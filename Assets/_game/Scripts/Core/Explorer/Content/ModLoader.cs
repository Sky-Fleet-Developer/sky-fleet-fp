using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ContentSerializer;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Explorer.Content
{
    /// <summary>
    /// Класс отвечает за первичную загрузку мода - чтение dll, загрузка превью.
    /// </summary>
    public class ModLoader
    {
        LinkedList<Mesh> meshLoaded;
        LinkedList<Texture2D> textureLoaded;

        public async Task<Mod> Read(string path)
        {
            string fileDefineMod = File.ReadAllText(path + "/" + PathStorage.BASE_MOD_FILE_DEFINE);
            SerializationModule module = JsonConvert.DeserializeObject<SerializationModule>(fileDefineMod);
            
            Assembly assembly = ReadAssembly(path + "/" + PathStorage.ASSEMBLY_FILE_DEFINE);

            return new Mod(path, module, assembly);
        }
        
        private Assembly ReadAssembly(string path)
        {
            return System.AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path));
            /*
            if (asm != null)
            {
                //loadedLog.Add($"Found assamble {asm.GetName().Name}:");

                Type[] types = asm.GetTypes();

                foreach (Type type in types)
                {
                    //loadedLog.Add(type.FullName);
                }
            }
            else
            {
                //loadedLog.Add($"Assamble is null - {asm.GetName().Name}:");
            }
            */
        }

        /*private Mesh ReadMesh(string pathMesh)
        {
            try
            {
                FileStream meshFile = File.Open(pathMesh, FileMode.Open);

                Mesh mesh = new Mesh();
                _ = MeshSerializer.Deserialize(meshFile, mesh);
                meshFile.Close();
                return mesh;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Cant find mesh at path" + pathMesh);
                Debug.LogError(e);
                return null;
            }
        }

        private Texture2D ReadTexture(string pathTexture)
        {
            Task<Texture2D> task = Texture2DCreator.CreateInstance(pathTexture);
            Task.WaitAll(task);
            return task.Result;
        }*/
    }
}