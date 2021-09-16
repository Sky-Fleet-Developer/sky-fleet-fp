using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

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