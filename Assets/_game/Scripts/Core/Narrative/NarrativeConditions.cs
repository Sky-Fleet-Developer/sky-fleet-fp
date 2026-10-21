using QnE;

namespace Core.Narrative
{
    /// <summary>
    /// Game conditions for events. A condition only reads blackboard facts, so the event must also ask the
    /// matching bridge to publish them (for items: the <c>WatchInventory</c> command).
    /// </summary>
    public static class NarrativeConditions
    {
        public static ICondition HasItem(string inventoryKey, string itemId, int count)
        {
            return Conditions.Ge(NarrativeKeys.Inventory(inventoryKey, itemId), count);
        }
    }
}
