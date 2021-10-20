using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Newtonsoft.Json;

namespace Core.Explorer.Content
{
    /// <summary>
    /// Класс отвечает за первичную загрузку мода - чтение dll, загрузка превью.
    /// </summary>
    public class ModLoader
    {
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
        }
    }
}