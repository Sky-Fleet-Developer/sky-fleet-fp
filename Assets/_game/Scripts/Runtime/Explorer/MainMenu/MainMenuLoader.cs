using System.Threading.Tasks;
using Core.Explorer;
using Core.UIStructure;
using Core.UIStructure.Utilities;
using UnityEngine;
using Runtime.Explorer.Services;
using Zenject;

namespace Runtime.Explorer
{
    public class MainMenuLoader : MonoBehaviour, ILoadAtStart
    {
        [Inject] private ServiceIssue _serviceIssue;
        public Task Load()
        {
            var menu = _serviceIssue.CreateService<Window, MainMenu>();
            menu.Window.RectTransform.Fullscreen();
            menu.RectTransform.Fullscreen();
            return Task.CompletedTask;
        }
    }
}
