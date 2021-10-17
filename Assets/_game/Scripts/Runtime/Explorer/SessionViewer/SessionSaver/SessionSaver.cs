using System.Collections.Generic;
using System.Linq;
using Core.Explorer.Content;
using Core.SessionManager;
using Core.SessionManager.SaveService;
using Core.UiStructure;
using Core.Utilities;
using Core.Utilities.UI;
using Core.ContentSerializer;

using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.SessionViewer.SessionLoader
{
    public class SessionSaver : UiBlockBase
    {
        [SerializeField] private SessionFilerManager sessionFilerManager;

        [SerializeField] private Button saveButton;

        [SerializeField] private InputField nameSession;

        private void Start()
        {
            sessionFilerManager.SetStartPath(PathStorage.GetPathToSessionSave());
            sessionFilerManager.UpdateFileMandager();
            saveButton.onClick.AddListener(SaveSession);
        }


        private void SaveSession()
        {
            if (string.IsNullOrEmpty(nameSession.text))
            { return; }
            SaveLoadUtility saveLoad = new SaveLoadUtility();
            saveLoad.SaveSession(sessionFilerManager.GetCurrentPath(), nameSession.text);
        }
    }
}