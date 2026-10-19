using System.IO;

namespace Core.World
{
    /// <summary>
    /// Tells the world where runtime changes of a location are stored, so the authored content is never overwritten.
    /// </summary>
    public interface IWorldSaveSlot
    {
        /// <returns>Root directory of the current save slot.</returns>
        string GetSlotDirectory();

        /// <returns>Directory for the given location inside the current save slot.</returns>
        string GetLocationDirectory(string locationName);
    }

    /// <summary>
    /// Single fixed slot under the session save folder. Replace it when the session context appears.
    /// </summary>
    public class DefaultWorldSaveSlot : IWorldSaveSlot
    {
        private const string WorldFolder = "World";
        private const string DefaultSlotName = "Default";

        public string GetSlotDirectory()
        {
            return Path.Combine(PathStorage.GetPathToSessionSave(), DefaultSlotName);
        }

        public string GetLocationDirectory(string locationName)
        {
            return Path.Combine(GetSlotDirectory(), WorldFolder, locationName);
        }
    }
}
