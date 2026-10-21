using System.Collections.Generic;
using Core;
using Core.Narrative;
using QnE;
using QnE.Unity;
using Runtime.Narrative.Authoring;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Runtime.Narrative
{
    /// <summary>
    /// Hands out one quest: offers the source with these arguments, from the inspector button or when the game has
    /// loaded. A stand-in for a quest giver until one exists.
    /// </summary>
    public class QuestOffer : MonoBehaviour
    {
        [SerializeField] private EventDefinitionAsset source;
        [SerializeField] private List<ArgumentAsset> args = new();
        [SerializeField, PropertyTooltip("Offers after the game has loaded, unless a copy of the quest is already running.")]
        private bool offerOnStart;

        [Inject] private IQuestsAndEvents _narrative;

        private void Start()
        {
            if (offerOnStart) Bootstrapper.OnLoadComplete.Subscribe(Offer);
        }

        [Button]
        public void Offer()
        {
            if (_narrative == null || !source) return;
            if (_narrative.Service.HasRunningOffer(source.SourceId)) return;

            var offerArgs = new Dictionary<string, QValue>();
            foreach (var arg in args) offerArgs[arg.name] = arg.value.Build();
            _narrative.Offer(source, offerArgs);
        }
    }
}
