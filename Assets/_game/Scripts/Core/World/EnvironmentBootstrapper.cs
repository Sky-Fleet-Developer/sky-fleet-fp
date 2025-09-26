using System.Threading.Tasks;
using Core.Boot_strapper;
using UnityEngine;

namespace Core.World
{
    public class EnvironmentBootstrapper : MonoBehaviour, ILoadAtStart
    {

        public Task Load()
        {
           /* LinkedList<Mod> mods = Session.Instance.GetMods();

            List<Assembly> allAssemblies = System.AppDomain.CurrentDomain.GetAssemblies().ToList();
            
            foreach (Mod mod in mods)
            {
                allAssemblies.Add(mod.assembly);   
            }

            List<IEnvironmentSystem> environmentSystems = new List<IEnvironmentSystem>();
            
            foreach (Assembly allAssembly in allAssemblies)
            {
                
            }*/
            
            return Task.CompletedTask;
        }
    }
}
