using System;
using System.Collections;
using Core.UIStructure;
using Core.Utilities;
using DG.Tweening;
using UnityEngine;

namespace Core.UiStructure
{
    public interface IService
    {
        MonoBehaviour Self { get; }
        Window Window { get; set; }
        BearerCanvas Bearer { get; set; }
        RectTransform RectTransform { get; }
        IEnumerator Show(BlockSequenceSettings settings = null);
        IEnumerator Hide(BlockSequenceSettings settings = null);
    }

    public class Service : MonoBehaviour, IService
    {
        public MonoBehaviour Self => this;
        public Window Window { get; set; }
        public BearerCanvas Bearer { get; set; }
        public static IService FocusBlock { get; private set; }
        public static event Action<IService> OnBlockWasFocused;

        public static void FocusOn(IService block)
        {
            if (FocusBlock == block) return;
            OnBlockWasFocused?.Invoke(block);
            FocusBlock = block;
        }

        public RectTransform RectTransform => rectTransform;
        private RectTransform rectTransform;

        public bool setAsFocusedOnShow = false;
        public DOTweenTransition showTransition;
        public DOTweenTransition hideTransition;

        protected virtual void Awake()
        {
            Bearer = GetComponentInParent<BearerCanvas>();
            rectTransform = transform as RectTransform;
            OnBlockWasFocused += OnBlockFocusChanged;
        }

        protected virtual void OnDestroy()
        {
            OnBlockWasFocused -= OnBlockFocusChanged;
            if (Bearer != null)
            {
                Bearer.Remove(this);
            }
        }

        protected virtual void OnBlockFocusChanged(IService block)
        {
            if (block == null) return;
            if (gameObject.activeSelf && setAsFocusedOnShow && (Service) block != this)
            {
                StartCoroutine(Hide());
            }
        }

        public virtual IEnumerator Show(BlockSequenceSettings settings = null)
        {
            if (setAsFocusedOnShow) FocusOn(this);
            if (settings == null) yield return showTransition.Setup(Vector3.one, rectTransform.DOScale).SetUpdate(UpdateType.Normal, true).WaitForCompletion();
            else yield return settings.ApplySequenceSettings(this);
        }

        public virtual IEnumerator Hide(BlockSequenceSettings settings = null)
        {
            if (settings == null) yield return hideTransition.Setup(Vector3.zero, rectTransform.DOScale).SetUpdate(UpdateType.Normal, true).WaitForCompletion();
            else yield return settings.ApplySequenceSettings(this);
            DynamicPool.Instance.Return(this);
            if ((Service) FocusBlock == this) FocusOn(null);
        }

        /*public static T Show<T>(T prefab, BearerCanvas bearer, BlockSequenceSettings settings = null) where T : MonoBehaviour, IService
        {
            T instance;
            if (prefab is Window == false && bearer.GetBlock<T>(out T block, prefab.GetType()))
            {
                instance = block;
            }
            else
            {
                instance = DynamicPool.Instance.Get(prefab, bearer.transform);
            }
            instance.gameObject.SetActive(true);
            instance.transform.localPosition = Vector3.zero;
            bearer.StartCoroutine(instance.Show(settings));
            return instance;
        }*/

    }

    public abstract class BlockSequenceSettings
    {
        public abstract IEnumerator ApplySequenceSettings(Service block);
    }
}
