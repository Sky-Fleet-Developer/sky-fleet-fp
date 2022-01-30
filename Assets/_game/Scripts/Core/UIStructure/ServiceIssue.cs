using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.UiStructure;
using Core.Utilities;
using UnityEngine;

namespace Core.UIStructure
{
    public class ServiceIssue : MonoBehaviour, ILoadAtStart
    {
        public static ServiceIssue Instance;
        [SerializeField] private BearerCanvas bearerPrefab;
        [SerializeField] private List<Service> handMadeServices;
        [SerializeField] private List<Window> availableFrames;
        private List<IService> availableServices = new List<IService>();


        public Task Load()
        {
            if (Instance)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(Instance);
                availableServices.AddRange(handMadeServices);
            }

            return Task.CompletedTask;
        }

        public void AddService(params IService[] services)
        {
            availableServices.AddRange(services);
        }

        public BearerCanvas CreateBearer()
        {
            return DynamicPool.Instance.Get(bearerPrefab);
        }

        public T CreateWindow<T>() where T : Window
        {
            BearerCanvas bearer = CreateBearer();
            return (T)bearer.CreateWindow(GetFramePrefab<T>());
        }

        public List<IService> GetOrMakeServices<TWindow>(Window.LayoutType layoutType = Window.LayoutType.Horizontal, params Type[] services) where TWindow : Window
        {
            var window = CreateWindow<TWindow>();
            window.SetLayout(layoutType);
            return GetOrMakeServices(window, 0, services);
        }

        public T GetOrMakeService<T>(Window.LayoutType layoutType = Window.LayoutType.None) where T : MonoBehaviour, IService
        {
            BearerCanvas bearer = CreateBearer();
            Window window = bearer.CreateWindow(availableFrames[0]);
            window.SetLayout(layoutType);
            return GetOrMakeService<T>(window);
        }

        public TService GetOrMakeService<TWindow, TService>(Window.LayoutType layoutType = Window.LayoutType.None) where TService : MonoBehaviour, IService where TWindow : Window
        {
            Window window = CreateWindow<TWindow>();
            window.SetLayout(layoutType);
            return GetOrMakeService<TService>(window);
        }

        public List<IService> GetOrMakeServices(Window window, int index, params Type[] services)
        {
            List<IService> instances = new List<IService>();
            foreach (Type type in services)
            {
                IService prefab = GetServicePrefab(x => x.GetType() == type);
                IService result = window.Bearer.Create(prefab, window);
                instances.Add(result);
            }
            
            window.Append(index, instances.ToArray());
            
            return instances;
        }
        
        public T GetOrMakeService<T>(Window window, int index = 0) where T : MonoBehaviour, IService
        {
            Type type = typeof(T);
            T prefab = (T) GetServicePrefab(x => x.GetType() == type);
            T result = window.Bearer.Create(prefab, window);
            return result;
        }


        public IService GetServicePrefab(Func<IService, bool> selector)
        {
            return availableServices.FirstOrDefault(selector);
        }
        
        public Window GetFramePrefab<T>() where T : Window
        {
            Type t = typeof(T);
            return availableFrames.FirstOrDefault(x => x.GetType() == t);
        }
    }
}
