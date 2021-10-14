using System;
using System.Collections;
using System.Collections.Generic;
using Core.UiStructure;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UIStructure
{
    public interface IUiStructure
    {
        Transform transform { get; }
        Canvas Canvas { get; }
        CanvasScaler Scaler { get; }
        Coroutine StartCoroutine(IEnumerator enumerator);
        List<IUiBlock> GetParentBlocks();
        bool GetBlock<T>(out T block) where T : IUiBlock;
        bool GetBlock<T>(out T block, Type type) where T : IUiBlock;
        T Instantiate<T>(T prefab, BlockSequenceSettings settings = null) where T : MonoBehaviour, IUiBlock;
        void Insert<T>(T instance) where T : MonoBehaviour, IUiBlock;
        void Remove(IUiBlock block);
        void RefreshBlocks();
    }
}
