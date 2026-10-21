using System.Globalization;
using Core.Misc;
using Core.Structure;

namespace Core.Items
{
    /// <summary>
    /// An item object (a block of a structure, a unit...) that has live state worth saving.
    /// Implemented by the object itself (one per object). The state lives in the properties of the object's <see cref="ItemInstance"/>, so it travels with the
    /// <see cref="ItemDescription"/> and is applied again when the object is created.
    /// </summary>
    public interface IStatefulItemObject
    {
        /// <summary>Part of the property name.</summary>
        string StateKey { get; }
        /// <returns>Text owned by the component (keep it backward compatible), or null if there is nothing to save.</returns>
        string CaptureState();
        void RestoreState(string state);
    }

    public static class ItemObjectState
    {
        public const string PropertyPrefix = "state.";

        public static string FloatToState(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        public static bool TryParseFloat(string state, out float value) =>
            float.TryParse(state, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        /// <summary>
        /// Writes the live state of the object into its item instance. If the object is a structure, its blocks are
        /// captured too (blocks exist only inside structures, so there is no search through the hierarchy).
        /// Captured: player properties of blocks and the state of <see cref="IStatefulItemObject"/>.
        /// </summary>
        public static void Capture(IItemObject root)
        {
            CaptureSingle(root);
            if (root is not IStructure structure) return;
            foreach (var block in structure.Blocks)
            {
                CaptureSingle(block);
            }
        }

        private static void CaptureSingle(IItemObject itemObject)
        {
            var item = itemObject.SourceItem;
            if (item == null) return;
            if (itemObject is IBlock block)
            {
                foreach (var propertyInfo in block.GetBlockPlayerPropertiesCached())
                {
                    var value = propertyInfo.GetValue(block);
                    if (value != null)
                    {
                        item.SetProperty(propertyInfo.Name, new PropertyValue(value.ToString()));
                    }
                }
            }

            if (itemObject is IStatefulItemObject stateful)
            {
                var state = stateful.CaptureState();
                if (state != null)
                {
                    item.SetProperty(PropertyPrefix + stateful.StateKey, new PropertyValue(state));
                }
            }
        }

        /// <summary>
        /// Gives the saved state back to a freshly created object.
        /// </summary>
        public static void Restore(IItemObject itemObject, ItemInstance item)
        {
            if (itemObject is IStatefulItemObject stateful
                && item.TryGetProperty(PropertyPrefix + stateful.StateKey, out var property)
                && property.values is { Length: > 0 })
            {
                stateful.RestoreState(property.values[0].stringValue);
            }
        }
    }
}
