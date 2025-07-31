using System.Threading.Tasks;
using Core.Boot_strapper;
using UnityEngine;

namespace Core.Structure.Infrastructure
{
    public class InfrastructureCombiner : MonoBehaviour, ILoadAtStart
    {
        public Task Load()
        {
        
            return Task.CompletedTask;
        }
    }
}
