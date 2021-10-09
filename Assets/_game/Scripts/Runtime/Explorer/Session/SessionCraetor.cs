using Core.UiStructure;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core.Explorer.Content;
using Core.Session;

namespace Runtime.Explorer.ModContent
{
    public class SessionCraetor : UiBlockBase
    {
        [SerializeField] private Button takeModAll;
        [SerializeField] private Button putModAll;

        [SerializeField] private Button takeModOne;
        [SerializeField] private Button putModOne;

        [SerializeField] private InputField nameSessionField;
        
        [SerializeField] private Button startSession;

        [Space(10)]
        [SerializeField] private SessionModInfo sessionModInfo;

        private ModViewer modViewer;

        private LinkedList<Mod> mods = new LinkedList<Mod>();


        private void Start()
        {
            modViewer = GetModViewer();
            startSession.onClick.AddListener(CallStartSession);

            takeModAll.onClick.AddListener(TakeAllMods);
            putModAll.onClick.AddListener(PutAllMods);
            takeModOne.onClick.AddListener(TakeOneMod);
            putModOne.onClick.AddListener(PutOneMod);
        }

        protected override void Awake()
        {
            base.Awake(); 
        }

        ModViewer GetModViewer()
        {
            ModViewer block;
            if(Frame.GetBlock<ModViewer>(out block))
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
            SelectorScenes.StartLoadSession();
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