using System;
using System.Collections;
using System.Collections.Generic;
using Core.Utilities;
using DG.Tweening;
using UnityEngine;

namespace  Core.UiStructure
{
    public interface IUiBlock
    {
        RectTransform RectTransform { get; }
        IEnumerator Show(BlockSequenceSettings settings = null);
        IEnumerator Hide(BlockSequenceSettings settings = null);
    }

    public class UiBlockBase : MonoBehaviour, IUiBlock
    {
        public RectTransform RectTransform => rectTransform;
        private RectTransform rectTransform;

        public DOTweenTransition showTransition;
        public DOTweenTransition hideTransition;

        protected virtual void Awake()
        {
            rectTransform = transform as RectTransform;
        }

        public IEnumerator Show(BlockSequenceSettings settings = null)
        {
            if(settings == null) yield return showTransition.Setup(Vector3.one, rectTransform.DOScale).WaitForCompletion();
            else yield return settings.ApplySequenceSettings(this);
        }

        public IEnumerator Hide(BlockSequenceSettings settings = null)
        {
            if(settings == null) yield return hideTransition.Setup(Vector3.zero, rectTransform.DOScale).WaitForCompletion();
            else yield return settings.ApplySequenceSettings(this);
        }
    }

    public abstract class BlockSequenceSettings
    {
        public abstract IEnumerator ApplySequenceSettings(IUiBlock block);
    }
}
