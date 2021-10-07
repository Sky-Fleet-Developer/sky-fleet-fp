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
        foreach (var uiBlockButton in menus)
        {
            var buttonInstance = DynamicPool.Instance.Get(buttonSource, transform);
            uiBlockButton.Apply(buttonInstance, _structure, OnBlockWasOpened);
            buttonInstance.SetVisual(fontSize);
            buttons.Add(buttonInstance);
        }
        return Task.CompletedTask;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (var buttonItemPointer in buttons)
        {
            buttonItemPointer.ResetVisual();
            DynamicPool.Instance.Return(buttonItemPointer);
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

    private void OnBlockWasOpened(UiBlockBase block)
    {
        //StartCoroutine(Hide());
        var frame = DynamicPool.Instance.Get(framePrefab);
        frame.Apply(block);
    }
    
    [System.Serializable]
    public class UIBlockButton
    {
        public UiBlockBase block;
        public string description;
        public FontStyle style;
        public TextAnchor alignment;

        private System.Action<UiBlockBase> onBlockWasOpen;

        public void Apply(ButtonItemPointer button, IUiStructure structure, System.Action<UiBlockBase> onBlockWasOpen)
        {
            this.onBlockWasOpen = onBlockWasOpen;
            var action = (System.Action)(() => OpenBlock(structure));
            button.SetVisual(description, style, alignment, action);
        }

        private void OpenBlock(IUiStructure structure)
        {
            var instance = structure.Instantiate(block);
            onBlockWasOpen?.Invoke(instance);
        }
    }
}
