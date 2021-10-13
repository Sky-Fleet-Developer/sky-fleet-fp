using System.Collections.Generic;
using System.Linq;
using Core.SessionManager;
using Core.UiStructure;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UIStructure
{
    [RequireComponent(typeof(Canvas))]
    public class UiStructureBase : MonoBehaviour, IUiStructure
    {
        public Canvas Canvas => canvas;
        public CanvasScaler Scaler => scaler;

        private Canvas canvas;
        private CanvasScaler scaler;
        private GraphicRaycaster graphicRaycaster;
        [ShowInInspector, ReadOnly] private List<IUiBlock> blocks;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            scaler = GetComponent<CanvasScaler>();
            graphicRaycaster = GetComponent<GraphicRaycaster>();
            RefreshBlocks();

            SceneLoader.StartChangeScene += BlockInterfaceControl;
        }

        private void OnDestroy()
        {
            SceneLoader.StartChangeScene -= BlockInterfaceControl;
        }

        public void RefreshBlocks()
        {
            blocks = GetComponentsInChildren<IUiBlock>().ToList();
        }

        public List<IUiBlock> GetParentBlocks()
        {
            return blocks;
        }

        public bool GetBlock<T>(out T block) where T : IUiBlock
        {
            block = (T)blocks.FirstOrDefault(x => x.GetType() == typeof(T));
            return block != null;
        }

        public bool GetBlock<T>(out T block, System.Type type) where T : IUiBlock
        {
            block = (T)blocks.FirstOrDefault(x => x.GetType() == type);
            return block != null;
        }

        public void Insert<T>(T instance) where T : MonoBehaviour, IUiBlock
        {
            blocks.Add(instance);
        }
        public T Instantiate<T>(T prefab, BlockSequenceSettings settings = null) where T : MonoBehaviour, IUiBlock
        {
            T block = UiBlockBase.Show(prefab, this, settings);
            block.Structure = this;
            blocks.Add(block);
            return block;
        }
        public void Remove(IUiBlock block)
        {
            blocks.Remove(block);
        }

        private void BlockInterfaceControl()
        {
            graphicRaycaster.enabled = false;
        }
    }
}
