using System.Collections.Generic;
using QnE;

namespace Core.Narrative
{
    /// <summary>
    /// Access to the running quests and events system for the rest of the game.
    /// </summary>
    public interface IQuestsAndEvents
    {
        QnEService Service { get; }
        /// <summary>Bridge to world entities; null if the world has no entity index.</summary>
        NarrativeActors Actors { get; }
        /// <summary>
        /// Starts an event built by the source for these arguments, for example a quest a giver hands out.
        /// The source must be listed in the service, so a saved game can rebuild the event.
        /// </summary>
        EventInstance Offer(IEventDefinitionSource source, IDictionary<string, QValue> args = null, string entry = null);
        /// <summary>Writes running events, blackboard, timers and counters to the current save slot.</summary>
        void Save();
    }
}
