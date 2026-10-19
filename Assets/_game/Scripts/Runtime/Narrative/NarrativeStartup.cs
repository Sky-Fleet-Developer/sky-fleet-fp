using Core;
using Core.Narrative;
using QnE;
using UnityEngine;
using Zenject;

namespace Runtime.Narrative
{
    /// <summary>
    /// Starts the first event of a new game. The flag lives in the blackboard, so it is saved with the narrative state
    /// and a loaded game does not start the event again.
    /// </summary>
    public class NarrativeStartup : MonoBehaviour
    {
        public const string StartedKey = "game.started";

        [SerializeField] private string startEventId;
        [SerializeField, Tooltip("Optional entry point of the start event.")] private string startEntry;

        [Inject] private IQuestsAndEvents _narrative;

        private void Start()
        {
            Bootstrapper.OnLoadComplete.Subscribe(StartIfNewGame);
        }

        private void StartIfNewGame()
        {
            if (_narrative == null || string.IsNullOrEmpty(startEventId)) return;
            var blackboard = _narrative.Service.Blackboard;
            if (blackboard.Get(StartedKey).AsBool()) return;
            blackboard.Set(StartedKey, true);
            _narrative.Service.Launch(startEventId, string.IsNullOrEmpty(startEntry) ? null : startEntry);
        }
    }
}
