using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using ContentSerializer;

namespace Core.Mods
{
    public class ModReader
    {
        LinkedList<Mesh> meshLoaded;
        LinkedList<Texture2D> textureLoaded;

        public Mod Read(string path)
        {
            string fileDefineMod = File.ReadAllText(path + "/" + PathStorage.BASE_MOD_FILE_DEFINE);
            SerializationModule modul = (SerializationModule)JsonConvert.DeserializeObject(fileDefineMod);
            return null;
        }

        private Mesh ReadMesh(string pathMesh)
        {
            FileStream meshFile = File.Open(pathMesh, FileMode.Open);
            if(meshFile != null)
            {
                Mesh mesh = new Mesh();
                _ = MeshSerializer.Deserialize(meshFile, mesh);
                meshFile.Close();
                return mesh;
            }
            return null;
        }

        private Texture2D ReadTexture(string pathTexture)
        {
            Task<Texture2D> task = Texture2DCreator.CreateInstance(pathTexture);
            Task.WaitAll(task);
            return task.Result;
        }
    }
}