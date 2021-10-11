using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Core.Utilities;
using Core.Utilities.UI;
using Core.Explorer.Content;
using Core.SessionManager;
using Core.UiStructure;
using Core.SessionManager.SaveService;

using Runtime.Explorer.ModContent;


namespace Runtime.Explorer.SessionViewer
{
    public class SessionLoader : UiBlockBase
    {
        [SerializeField] private SessionFilerManager sessionFilerManager;

        [SerializeField] private Button startButton;


        [Space(10)]
        [SerializeField] private ButtonItemPointer prefabItem;

        [SerializeField] private Transform content;

        private LinkedList<ButtonItemPointer> items = new LinkedList<ButtonItemPointer>();

        private SaveLoad.LoadSettingSession takeSessionSave;

        private void Start()
        {
            sessionFilerManager.SelectFile += TakeSession;
            startButton.onClick.AddListener(StartSession);
        }

        private void TakeSession(string path)
        {
            SaveLoad saveLoad = new SaveLoad();
            takeSessionSave = saveLoad.LoadBaseInfoSession(path);
            ClearList();
            foreach(string modN in takeSessionSave.NoHaveMods)
            {
                ButtonItemPointer item = DynamicPool.Instance.Get(prefabItem, content);
                items.AddLast(item);
                item.SetVisual("! - " + modN);
            }
            foreach (Mod mod in takeSessionSave.mods)
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
            if (takeSessionSave == null)
                return;
            Session.Instance.BeginInit();
            Session.Instance.SetSettings(takeSessionSave);
            Session.Instance.EndInit();
            SceneLoader.LoadGameScene();
        }


    }
}