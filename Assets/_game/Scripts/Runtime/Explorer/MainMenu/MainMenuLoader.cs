using System;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.UIStructure;
using Core.UIStructure.Utilities;
using UnityEngine;
using Runtime.Explorer.Services;

namespace Runtime.Explorer
{
    public class MainMenuLoader : MonoBehaviour, ILoadAtStart
    {
        public Task Load()
        {
            var menu = ServiceIssue.Instance.CreateService<Window, MainMenu>();
            menu.Window.RectTransform.Fullscreen();
            menu.RectTransform.Fullscreen();
            return Task.CompletedTask;
        }
    }
}
