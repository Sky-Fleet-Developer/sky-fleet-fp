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
        /// <summary>Writes running events, blackboard, timers and counters to the current save slot.</summary>
        void Save();
    }
}
