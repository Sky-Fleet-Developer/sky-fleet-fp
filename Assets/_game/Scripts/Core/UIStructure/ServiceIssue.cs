using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.UiStructure;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Core.UIStructure
{
    public class ServiceIssue : MonoBehaviour, IMyInstaller, ILoadAtStart
    {
        public static ServiceIssue Instance;
        [SerializeField] private BearerCanvas bearerPrefab;
        [SerializeField] private List<Service> handMadeServices;
        [SerializeField] private List<Window> availableFrames;
        private List<IService> _availableServices = new List<IService>();

        public void InstallBindings(DiContainer container)
        {
            container.Bind<ServiceIssue>().FromInstance(this);
        }

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
                _availableServices.AddRange(handMadeServices);
            }

            return Task.CompletedTask;
        }

        public void AddService(params IService[] services)
        {
            _availableServices.AddRange(services);
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
        
        public Window CreateWindow(Type type)
        {
            BearerCanvas bearer = CreateBearer();
            return bearer.CreateWindow(GetFramePrefab(type));
        }

        public List<IService> CreateServices<TWindow>(Window.LayoutType layoutType = Window.LayoutType.Horizontal, params Type[] services) where TWindow : Window
        {
            var window = CreateWindow<TWindow>();
            window.SetLayout(layoutType);
            return CreateServices(window, 0, services);
        }

        public IService CreateService(Type serviceType, Type frameType, Window.LayoutType layoutType = Window.LayoutType.None)
        {
            Window window = CreateWindow(frameType);
            window.SetLayout(layoutType);
            return CreateService(serviceType, window);
        }
        
        public IService CreateService(Type serviceType, Window.LayoutType layoutType = Window.LayoutType.None)
        {
            BearerCanvas bearer = CreateBearer();
            Window window = bearer.CreateWindow(availableFrames[0]);
            window.SetLayout(layoutType);
            return CreateService(serviceType, window);
        }
        
        public T CreateService<T>(Window.LayoutType layoutType = Window.LayoutType.None) where T : MonoBehaviour, IService
        {
            BearerCanvas bearer = CreateBearer();
            Window window = bearer.CreateWindow(availableFrames[0]);
            window.SetLayout(layoutType);
            return CreateService<T>(window);
        }

        public TService CreateService<TWindow, TService>(Window.LayoutType layoutType = Window.LayoutType.None) where TService : MonoBehaviour, IService where TWindow : Window
        {
            Window window = CreateWindow<TWindow>();
            window.SetLayout(layoutType);
            return CreateService<TService>(window);
        }

        public List<IService> CreateServices(Window window, int index, params Type[] services)
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
        
        public T CreateService<T>(Window window, int index = 0) where T : MonoBehaviour, IService
        {
            Type type = typeof(T);
            return (T)CreateService(type, window, index);
        }
        public IService CreateService(Type type, Window window, int index = 0)
        {
            IService prefab = GetServicePrefab(x => x.GetType() == type);
            IService result = window.Bearer.Create(prefab, window);
            return result;
        }

        public IService GetServicePrefab(Func<IService, bool> selector)
        {
            return _availableServices.FirstOrDefault(selector);
        }
        
        public Window GetFramePrefab<T>() where T : Window
        {
            Type t = typeof(T);
            return GetFramePrefab(t);
        }
        public Window GetFramePrefab(Type t)
        {
            return availableFrames.FirstOrDefault(x => x.GetType() == t);
        }
    }
}
