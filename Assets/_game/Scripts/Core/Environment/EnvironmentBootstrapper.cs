using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Explorer.Content;
using Core.SessionManager;
using UnityEngine;

namespace Core.Environment
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
