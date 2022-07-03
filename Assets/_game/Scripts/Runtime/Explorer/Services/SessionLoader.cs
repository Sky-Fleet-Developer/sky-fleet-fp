using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Explorer.Content;
using Core.SessionManager;
using Core.SessionManager.SaveService;
using Core.UiStructure;
using Core.UIStructure.Utilities;
using Core.Utilities;
using Runtime.Explorer.SessionViewer;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.Services
{
    public class SessionLoader : Service
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
            sessionFilerManager.SetStartPath(PathStorage.GetPathToSessionSave());
            sessionFilerManager.UpdateFileMandager();
            sessionFilerManager.SelectFile += TakeSession;
            startButton.onClick.AddListener(StartSession);
        }

        private void TakeSession(string path)
        {
            SaveLoad saveLoad = new SaveLoad();
            currentSessionPath = path;
            StateHeader header = saveLoad.ReadHeader(path);
            currentSessionSettings = GetSettingsFromHeader(header, out List<string> missingMods);
            ClearList();
            foreach (string missingMod in missingMods)
            {
                ButtonItemPointer item = DynamicPool.Instance.Get(prefabItem, content);
                items.AddLast(item);
                item.SetVisual($"<color=red>{missingMod}</color> - missing");
            }
            foreach (Mod mod in currentSessionSettings.mods)
            {
                ButtonItemPointer item = DynamicPool.Instance.Get(prefabItem, content);
                items.AddLast(item);
                item.SetVisual(mod.name);
            }
        }

        private SessionSettings GetSettingsFromHeader(StateHeader header, out List<string> missingMods)
        {
            SessionSettings settings = new SessionSettings();
            settings.name = header.name;

            missingMods = new List<string>();
            List<Mod> existingMods = ModReader.Instance.GetMods();
            foreach (string modName in header.mods)
            {
                Mod mod = existingMods.FirstOrDefault(x => x.name == modName);
                if (mod != null)
                {
                    settings.mods.AddLast(mod);
                }
                else
                {
                    missingMods.Add(modName);
                }
            }

            return settings;
        }

        private void ClearList()
        {
            foreach (ButtonItemPointer item in items)
            {
                DynamicPool.Instance.Return(item);
            }
            items.Clear();
        }

        private async void StartSession()
        {
            if (currentSessionSettings == null)
                return;
            Session.Instance.BeginInit();
            Session.Instance.SetSettings(currentSessionSettings);
            await SceneLoader.LoadGameScene();
            Session.Instance.Load(currentSessionPath, Session.Instance.EndInit);
        }

    }
}