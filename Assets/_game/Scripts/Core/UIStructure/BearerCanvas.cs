using System;
using System.Collections.Generic;
using System.Linq;
using Core.SessionManager;
using Core.UiStructure;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UIStructure
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public class BearerCanvas : MonoBehaviour, IPointerClickHandler
    {
        public static int Sorting;
        
        public Canvas Canvas => canvas;
        public CanvasScaler Scaler => scaler;

        private Canvas canvas;
        private CanvasScaler scaler;
        protected GraphicRaycaster graphicRaycaster;
        [ShowInInspector, ReadOnly] protected List<IService> blocks;
        
        protected virtual void Awake()
        {
            canvas = GetComponent<Canvas>();
            scaler = GetComponent<CanvasScaler>();
            graphicRaycaster = GetComponent<GraphicRaycaster>();
            Refresh();

            SceneLoader.StartChangeScene += BlockInterfaceControl;
        }

        protected void OnEnable()
        {
            Focus();
        }

        protected virtual void OnDestroy()
        {
            SceneLoader.StartChangeScene -= BlockInterfaceControl;
        }

        public void Refresh()
        {
            blocks = GetComponentsInChildren<IService>().ToList();
        }

        public List<IService> GetServices()
        {
            return blocks;
        }

        public bool GetBlock<T>(out T block) where T : IService
        {
            block = (T)blocks.FirstOrDefault(x => x.GetType() == typeof(T));
            return block != null;
        }

        public bool GetBlock<T>(out T block, System.Type type) where T : IService
        {
            block = (T)blocks.FirstOrDefault(x => x.GetType() == type);
            return block != null;
        }

        public void Insert<T>(T instance) where T : MonoBehaviour, IService
        {
            blocks.Add(instance);
        }
        
        public T CreateWindow<T>(T prefab) where T : MonoBehaviour, IService
        {
            var instance = DynamicPool.Instance.Get(prefab, transform);
            instance.gameObject.SetActive(true);
            
            instance.Bearer = this;
            blocks.Add(instance);
            return instance;
        }
        
        public T Create<T>(T prefab, Window window, int index = 0) where T : class, IService
        {
            MonoBehaviour instance = DynamicPool.Instance.Get(prefab.Self, window.Content);
            T serviceInstance = instance.GetComponent<T>();
            instance.gameObject.SetActive(true);
            window.Append(index, serviceInstance);

            serviceInstance.Bearer = this;
            blocks.Add(serviceInstance);
            return serviceInstance;
        }
        
        public void Remove(IService block)
        {
            blocks.Remove(block);
        }

        private void BlockInterfaceControl()
        {
            graphicRaycaster.enabled = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (canvas.sortingOrder < Sorting)
            {
                Focus();
            }
        }

        public void Focus()
        {
            Sorting++;
            canvas.sortingOrder = Sorting;
        }
    }
}
