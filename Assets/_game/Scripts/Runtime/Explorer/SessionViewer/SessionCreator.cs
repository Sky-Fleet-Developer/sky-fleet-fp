using System.Collections.Generic;
using System.IO;

using Core.Explorer.Content;
using Core.SessionManager;
using Core.UiStructure;
using Core;
using Core.SessionManager.SaveService;

using Runtime.Explorer.ModContent;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.SessionViewer
{
    public class SessionCreator : UiBlockBase
    {
        [SerializeField] private Button takeModAll;
        [SerializeField] private Button putModAll;

        [SerializeField] private Button takeModOne;
        [SerializeField] private Button putModOne;

        [SerializeField] private InputField nameSessionField;
        [SerializeField] private InputField presetSessionField;
        [SerializeField] private Toggle createDirectory;

        [SerializeField] private Button startSession;

        [Space(10)]
        [SerializeField] private SessionModInfo sessionModInfo;

        [Space(10)]
        [SerializeField] private SessionFilerManager sessionFilerManager;


        private ModViewer modViewer;

        private LinkedList<Mod> mods = new LinkedList<Mod>();

        private string takePreset;

        private void Start()
        {
            modViewer = GetModViewer();
            startSession.onClick.AddListener(CallStartSession);

            takeModAll.onClick.AddListener(TakeAllMods);
            putModAll.onClick.AddListener(PutAllMods);
            takeModOne.onClick.AddListener(TakeOneMod);
            putModOne.onClick.AddListener(PutOneMod);

            sessionFilerManager.SetStartPath(PathStorage.GetPathToSessionPresets());
            sessionFilerManager.UpdateFileMandager();
            sessionFilerManager.SelectFile += TakePreset;
        }

        protected override void Awake()
        {
            base.Awake(); 
        }

        private void TakePreset(string preset)
        {
            takePreset = preset;
            preset = Path.GetFileName(takePreset);
            preset = preset.Remove(preset.IndexOf('.'));
            presetSessionField.SetTextWithoutNotify(preset);
        }

        ModViewer GetModViewer()
        {
            ModViewer block;
            if(Frame.GetBlock(out block))
            {
                return block;
            }
            else
            {
                throw new System.Exception("пизда!");
            }
        }

        void CallStartSession()
        {
            Session.Instance.BeginInit();
            if(!string.IsNullOrEmpty(presetSessionField.text))
            {
                /*Load preset*/
            }
            string name = nameSessionField.text;
            if(createDirectory.isOn)
            {
                SaveLoadUtility saveLoadUtility = new SaveLoadUtility();
                name = saveLoadUtility.CreateDirectorySession(name);
            }
            Session.Instance.SetSettings(new SessionSettings
            {
                name = name,
                mods = mods
            });
            Session.Instance.EndInit();
            Debug.Log(Session.Instance.Settings.name);
            SceneLoader.LoadGameScene();
        }


        void TakeOneMod()
        {
            Mod select = modViewer.CurrentMod;
            if(select != null)
            {
                LinkedListNode<Mod> index = mods.Find(select);
                if(index == null)
                {
                    mods.AddLast(select);
                    sessionModInfo.AddModToList(select);
                }
            }
        }

        void PutOneMod()
        {
            mods.Remove(sessionModInfo.GetSelectMod);
            sessionModInfo.RemoveModFromList(sessionModInfo.GetSelectMod);
        }

        void TakeAllMods()
        {
            mods.Clear();
            foreach (Mod mod in ModReader.Instance.GetMods())
                mods.AddLast(mod);
            sessionModInfo.UpdateListMods(mods);
        }

        void PutAllMods()
        {
            mods.Clear();
            sessionModInfo.UpdateListMods(mods);
        }
    }
}