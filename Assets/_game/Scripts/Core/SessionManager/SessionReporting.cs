using System.Threading.Tasks;
using Core.Boot_strapper;
using UnityEngine;

namespace Core.SessionManager
{
    public class SessionReporting : MonoBehaviour, ILoadAtStart
    {
        public Task Load()
        {
            if(Session.Instance.IsInitialized())
            {
                Debug.Log("Session has a properties");
            }
            else
            {
                SessionNoHaveProperty();
            }
            return Task.CompletedTask;
        }

        private void SessionNoHaveProperty()
        {
            Debug.Log("Session hasn't a properties");
        }
            
    }
}