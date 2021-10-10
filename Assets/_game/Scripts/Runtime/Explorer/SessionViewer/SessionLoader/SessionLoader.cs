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
using Runtime.Explorer.ModContent;

namespace Runtime.Explorer.SessionViewer
{
    public class SessionLoader : UiBlockBase
    {
        [SerializeField] private SessionModInfo sessionModInfo;

        [SerializeField] private SessionFilerManager sessionFilerManager;


        [SerializeField] private Button startButton;


        private void Start()
        {
            
        }

        private void StartSession()
        {

        }


    }
}