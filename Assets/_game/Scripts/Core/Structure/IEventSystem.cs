using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Structure
{
    public interface IEventSystem
    {
        public Dictionary<string, List<(object owner, Action action)>> Events { get; }
        public Dictionary<string, List<(object owner, ActionBox action)>> EventsT { get; }

        public void Clear()
        {
            Events.Clear();
            EventsT.Clear();
        }
        
        public void AddEvent(string key, object owner, Action value)
        {
            if (!Events.TryGetValue(key, out List<(object owner, Action action)> list))
            {
                list = new List<(object owner, Action action)>(1);
                Events[key] = list;
            }
            list.Add((owner, value));
        }
        
        public void AddEvent<T>(string key, object owner, Action<T> value)
        {
            if (!EventsT.TryGetValue(key, out List<(object owner, ActionBox action)> list))
            {
                list = new List<(object owner, ActionBox action)>(1);
                EventsT[key] = list;
            }
            list.Add((owner, new ActionBox<T>(value)));
        }

        public void CallEvent(string key)
        {
            if (Events.TryGetValue(key, out List<(object owner, Action action)> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    (object owner, Action action) = list[i];
                    if (owner == null)
                    {
                        list.RemoveAt(i);
                        i--;
                        continue;
                    }
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
        }
        public void CallEvent<T>(string key, T value)
        {
            if (EventsT.TryGetValue(key, out List<(object owner, ActionBox action)> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    (object owner, ActionBox action) = list[i];
                    if (owner == null)
                    {
                        list.RemoveAt(i);
                        i--;
                        continue;
                    }
                    if (action is ActionBox<T> actionT)
                    {
                        try
                        {
                            actionT.Call(value);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
            }
        }
    }
}