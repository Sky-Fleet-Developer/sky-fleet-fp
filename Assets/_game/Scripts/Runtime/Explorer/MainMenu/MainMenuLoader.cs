using System;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.UIStructure;
using UnityEngine;

namespace Runtime.Explorer.MainMenu
{
    public class MainMenuLoader : MonoBehaviour, ILoadAtStart
    {
        public Task Load()
        {
            var menu = ServiceIssue.Instance.GetOrMakeService<Window, MainMenuService>();
            menu.Window.Fullscreen();
            return Task.CompletedTask;
        }
    }
}
