using Core;
using Core.SessionManager;
using Core.SessionManager.SaveService;
using Core.UiStructure;
using Runtime.Explorer.SessionViewer;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.Services
{
    public class SessionSaver : Service
    {
        [SerializeField] private SessionFilerManager sessionFilerManager;

        [SerializeField] private Button saveButton;

        [SerializeField] private InputField nameSession;

        private void Start()
        {
            sessionFilerManager.SetStartPath(PathStorage.GetPathToSessionSave());
            sessionFilerManager.UpdateFileManager();
            saveButton.onClick.AddListener(SaveSession);
        }


        private void SaveSession()
        {
            if (string.IsNullOrEmpty(nameSession.text))
            { return; }
            SaveLoadUtility saveLoad = new SaveLoadUtility();
            Session.Instance.Settings.name = nameSession.text;
            saveLoad.SaveSession(sessionFilerManager.GetCurrentPath(), nameSession.text);
        }
    }
}