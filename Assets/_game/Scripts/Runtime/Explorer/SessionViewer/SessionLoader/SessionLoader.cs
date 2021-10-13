using System.Collections.Generic;
using Core.Explorer.Content;
using Core.SessionManager;
using Core.SessionManager.SaveService;
using Core.UiStructure;
using Core.Utilities;
using Core.Utilities.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.SessionViewer.SessionLoader
{
    public class SessionLoader : UiBlockBase
    {
        [SerializeField] private SessionFilerManager sessionFilerManager;

        [SerializeField] private Button startButton;


        [Space(10)]
        [SerializeField] private ButtonItemPointer prefabItem;

        [SerializeField] private Transform content;

        private LinkedList<ButtonItemPointer> items = new LinkedList<ButtonItemPointer>();

        private SessionSettings currentSessionSettings;
        private string currentSessionPath;

        private void Start()
        {
            sessionFilerManager.SelectFile += TakeSession;
            startButton.onClick.AddListener(StartSession);
        }

        private void TakeSession(string path)
        {
            SaveLoad saveLoad = new SaveLoad();
            currentSessionPath = path;
            currentSessionSettings = saveLoad.ReadHeader(path);
            ClearList();
            foreach(string modN in currentSessionSettings.missingMods)
            {
                ButtonItemPointer item = DynamicPool.Instance.Get(prefabItem, content);
                items.AddLast(item);
                item.SetVisual($"<color=red>{modN}</color> - missing");
            }
            foreach (Mod mod in currentSessionSettings.mods)
            {
                ButtonItemPointer item = DynamicPool.Instance.Get(prefabItem, content);
                items.AddLast(item);
                item.SetVisual(mod.name);
            }
        }

        private void ClearList()
        {
            foreach(ButtonItemPointer item in items)
            {
                DynamicPool.Instance.Return(item);
            }
            items.Clear();
        }

        private void StartSession()
        {
            if (currentSessionSettings == null)
                return;
            Session.Instance.BeginInit();
            SceneLoader.LoadGameScene();
            Session.Instance.SetSettings(currentSessionSettings);
            Session.Instance.Load(currentSessionPath, Session.Instance.EndInit);
        }


    }
}