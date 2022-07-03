using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Explorer.Content
{
    /// <summary>
    /// Класс отвечает за первичную загрузку мода - чтение dll, загрузка превью.
    /// </summary>
    public class ModLoader
    {
        public Mod Read(string path)
        {
            try
            {
                string fileDefineMod = File.ReadAllText(path + "/" + PathStorage.BASE_MOD_FILE_DEFINE);
                SerializationModule module = JsonConvert.DeserializeObject<SerializationModule>(fileDefineMod);
            
                Assembly assembly = ReadAssembly(path + "/" + PathStorage.ASSEMBLY_FILE_DEFINE);
                return new Mod(path, module, assembly);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }
        
        private Assembly ReadAssembly(string path)
        {
#if  UNITY_EDITOR
            return AppDomain.CurrentDomain.Load(File.ReadAllBytes(path));
#endif
            return AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path));
        }
    }
}