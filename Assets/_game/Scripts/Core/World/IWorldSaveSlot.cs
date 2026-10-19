using System.IO;

namespace Core.World
{
    /// <summary>
    /// Tells the world where runtime changes of a location are stored, so the authored content is never overwritten.
    /// </summary>
    public interface IWorldSaveSlot
    {
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

        public string GetLocationDirectory(string locationName)
        {
            return Path.Combine(PathStorage.GetPathToSessionSave(), DefaultSlotName, WorldFolder, locationName);
        }
    }
}
