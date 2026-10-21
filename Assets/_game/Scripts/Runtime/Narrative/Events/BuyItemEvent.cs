using System.Collections.Generic;
using Core.Narrative;
using QnE;
using QnE.Unity;
using UnityEngine;

namespace Runtime.Narrative.Events
{
    /// <summary>
    /// "Get N of an item": completes when the inventory holds that many. The asset keeps the defaults, an offer can
    /// override them with the arguments <c>item</c>, <c>count</c> and <c>inventory</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SF/Narrative/Events/Buy Item", fileName = "BuyItem")]
    public class BuyItemEvent : ScriptedEventAsset
    {
        public const string ItemArg = "item";
        public const string CountArg = "count";
        public const string InventoryArg = "inventory";

        [SerializeField] private string defaultItemId;
        [SerializeField, Min(1)] private int defaultCount = 5;
        [SerializeField] private string defaultInventoryKey = "Player_Inventory_g:character_slots";

        protected override void Define(EventDef e, IReadOnlyDictionary<string, QValue> args)
        {
            string item = args.Text(ItemArg, defaultItemId);
            int count = args.Int(CountArg, defaultCount);
            string inventory = args.Text(InventoryArg, defaultInventoryKey);

            var buy = e.Root.Child("buy", $"Buy {count} {item}")
                .Do($"WatchInventory('{inventory}', '{item}')")
                .Do($"Log('Buy {count} x {item}')");
            var report = e.Root.Child("report")
                .Do($"Log('Done: {count} x {item}')")
                .Finish(StageOutcome.Complete);

            e.Root.Join = JoinMode.Any;
            e.Start(buy);
            buy.When(NarrativeConditions.HasItem(inventory, item, count), report);
        }
    }
}
