using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.UiStructure;
using Core.UIStructure;
using Core.Utilities;
using Core.Utilities.UI;
using Sirenix.OdinInspector;
using UnityEngine;

public class MainMenu : UiBlockBase, ILoadAtStart
{
    public List<UIBlockButton> menus;
    [FoldoutGroup("Style")]
    public ButtonItemPointer buttonSource;
    [FoldoutGroup("Style")]
    public int fontSize = 20;
    [FoldoutGroup("Style")] public UiFrame framePrefab;
    
    private IUiStructure _structure;
    private List<ButtonItemPointer> buttons = new List<ButtonItemPointer>();

    public Task Load()
    {
        _structure = GetComponentInParent<IUiStructure>();
        foreach (UIBlockButton uiBlockButton in menus)
        {
            ButtonItemPointer buttonInstance = DynamicPool.Instance.Get(buttonSource, transform);
            uiBlockButton.Apply(buttonInstance, _structure, OnBlockWasOpened);
            buttonInstance.SetVisual(fontSize);
            buttons.Add(buttonInstance);
        }
        return Task.CompletedTask;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (ButtonItemPointer buttonItemPointer in buttons)
        {
            buttonItemPointer.ResetVisual();
            if (DynamicPool.hasInstance)
            {
                DynamicPool.Instance.Return(buttonItemPointer);
            }
        }
    }

    protected override void OnBlockFocusChanged(IUiBlock block)
    {
        base.OnBlockFocusChanged(block);
        if (block == null)
        {
            gameObject.SetActive(true);
            StartCoroutine(Show());
        }
    }

    private void OnBlockWasOpened(UiBlockBase[] blocksBase)
    {
        //StartCoroutine(Hide());
        UiFrame frame = Structure.Instantiate(framePrefab);
        frame.Apply(UiFrame.LayoutType.Horizontal, blocksBase);
        FocusOn(frame);
    }
    
    [System.Serializable]
    public class UIBlockButton
    {
        public UiBlockBase[] blocks;
        public string description;
        public FontStyle style;
        public TextAnchor alignment;

        private System.Action<UiBlockBase[]> onBlockWasOpen;

        public void Apply(ButtonItemPointer button, IUiStructure structure, System.Action<UiBlockBase[]> onBlockWasOpen)
        {
            this.onBlockWasOpen = onBlockWasOpen;
            Action action = (System.Action)(() => OpenBlock(structure));
            button.SetVisual(description, style, alignment, action);
        }

        private void OpenBlock(IUiStructure structure)
        {
            UiBlockBase[] blocksBase = new UiBlockBase[blocks.Length];
            for(int i = 0; i < blocks.Length; i++)
            {
                blocksBase[i] = structure.Instantiate(blocks[i]);
            }
            onBlockWasOpen?.Invoke(blocksBase);
        }
    }
}
