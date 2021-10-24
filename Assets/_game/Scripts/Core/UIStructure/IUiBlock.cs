using System;
using System.Collections;
using Core.UIStructure;
using Core.Utilities;
using DG.Tweening;
using UnityEngine;

namespace Core.UiStructure
{
    public interface IUiBlock
    {
        UiFrame Frame { get; set; }
        IUiStructure Structure { get; set; }
        RectTransform RectTransform { get; }
        IEnumerator Show(BlockSequenceSettings settings = null);
        IEnumerator Hide(BlockSequenceSettings settings = null);
    }

    public class UiBlockBase : MonoBehaviour, IUiBlock
    {
        public UiFrame Frame { get; set; }
        public IUiStructure Structure { get; set; }
        public static IUiBlock FocusBlock { get; private set; }
        public static event Action<IUiBlock> OnBlockWasFocused;

        public static void FocusOn(IUiBlock block)
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
            Structure = GetComponentInParent<IUiStructure>();
            rectTransform = transform as RectTransform;
            OnBlockWasFocused += OnBlockFocusChanged;
        }

        protected virtual void OnDestroy()
        {
            OnBlockWasFocused -= OnBlockFocusChanged;
            if (Structure != null)
            {
                Structure.Remove(this);
            }
        }

        protected virtual void OnBlockFocusChanged(IUiBlock block)
        {
            if (block == null) return;
            if (gameObject.activeSelf && setAsFocusedOnShow && (UiBlockBase) block != this)
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
            gameObject.SetActive(false);
            if ((UiBlockBase) FocusBlock == this) FocusOn(null);
        }

        public static T Show<T>(T prefab, IUiStructure structure, BlockSequenceSettings settings = null) where T : MonoBehaviour, IUiBlock
        {
            T instance;
            if (prefab is UiFrame == false && structure.GetBlock<T>(out T block, prefab.GetType()))
            {
                instance = block;
            }
            else
            {
                instance = DynamicPool.Instance.Get(prefab, structure.transform);
            }
            instance.gameObject.SetActive(true);
            instance.transform.localPosition = Vector3.zero;
            structure.StartCoroutine(instance.Show(settings));
            return instance;
        }
    }

    public abstract class BlockSequenceSettings
    {
        public abstract IEnumerator ApplySequenceSettings(UiBlockBase block);
    }
}
