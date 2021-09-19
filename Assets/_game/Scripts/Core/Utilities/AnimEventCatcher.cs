using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Utilities
{
    public class AnimEventCatcher : MonoBehaviour
    {
        public AnimEvent[] actions;

        [System.Serializable]
        public class AnimEvent
        {
            public string name;
            public UnityEvent action;
        }

        public void AnimAction(string argument)
        {
            foreach(var hit in actions.Where(x => x.name == argument)) hit.action?.Invoke();
        }
    }
}