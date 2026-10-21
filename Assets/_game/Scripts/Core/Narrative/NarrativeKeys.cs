namespace Core.Narrative
{
    /// <summary>
    /// Names of the facts the game publishes to the narrative blackboard. Events read them, bridges write them,
    /// and this is the only place that knows the format.
    /// </summary>
    public static class NarrativeKeys
    {
        /// <summary>How many units of the item the inventory holds. Published only while an event watches it.</summary>
        public static string Inventory(string inventoryKey, string itemId) => $"inv.{inventoryKey}.{itemId}";
    }
}
