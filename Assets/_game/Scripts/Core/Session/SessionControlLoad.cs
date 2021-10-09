using Core.Boot_strapper;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Session
{
    public class SessionControlLoad : MonoBehaviour, ILoadAtStart
    {
        public Task Load()
        {
            if(SessionProperty.Instance.IsEndInitSessionProperty())
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