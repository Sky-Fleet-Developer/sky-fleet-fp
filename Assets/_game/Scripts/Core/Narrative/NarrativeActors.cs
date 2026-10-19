using System;
using System.Collections.Generic;
using Core.Misc;
using Core.World;
using QnE;

namespace Core.Narrative
{
    /// <summary>
    /// Text form of an entity key, as stored in the blackboard and in event arguments.
    /// </summary>
    public static class ActorRef
    {
        public static string Format(SmKey key) => $"{key.Index}:{key.Generation}";

        public static bool TryParse(string text, out SmKey key)
        {
            key = default;
            if (string.IsNullOrEmpty(text)) return false;
            int separator = text.IndexOf(':');
            if (separator <= 0) return false;
            if (!int.TryParse(text.Substring(0, separator), out int index) ||
                !int.TryParse(text.Substring(separator + 1), out int generation)) return false;
            key = new SmKey(index, generation);
            return true;
        }
    }

    /// <summary>
    /// Connects the narrative system to world entities without either side knowing the other's types.
    /// An event keeps its actors as blackboard values: <c>@.actor.giver = "12:3"</c> (an entity key),
    /// and the bridge publishes <c>@.actor.giver.alive</c>, so a stage can react when the entity is gone.
    /// </summary>
    public class NarrativeActors
    {
        private const string ActorMarker = ".actor.";
        private const string AliveSuffix = "alive";

        private readonly QnEService _service;
        private readonly IEntityIndex _index;
        private readonly List<KeyValuePair<string, bool>> _updates = new();

        public NarrativeActors(QnEService service, IEntityIndex index)
        {
            _service = service;
            _index = index;
        }

        /// <summary>
        /// BindActor(role, aliasOrKey) stores the actor in the event scope; UnbindActor(role) removes it.
        /// </summary>
        public void RegisterCommands()
        {
            _service.Commands.Register("BindActor", (context, args) =>
            {
                string role = args[0];
                if (!TryResolve(args[1], out var key))
                {
                    _service.Warning?.Invoke($"BindActor({role}): cannot resolve '{args[1]}'");
                    context.Set($"@.actor.{role}", string.Empty);
                    context.Set($"@.actor.{role}.{AliveSuffix}", false);
                    return null;
                }

                context.Set($"@.actor.{role}", ActorRef.Format(key));
                context.Set($"@.actor.{role}.{AliveSuffix}", true);
                return null;
            });
            _service.Commands.Register("UnbindActor", (context, args) =>
            {
                context.Blackboard.Remove(context.Resolve($"@.actor.{args[0]}"));
                context.Blackboard.Remove(context.Resolve($"@.actor.{args[0]}.{AliveSuffix}"));
                return null;
            });
        }

        /// <summary>
        /// Accepts an entity key ("12:3") or an authored alias. A key is accepted only while it is valid.
        /// </summary>
        public bool TryResolve(string value, out SmKey key)
        {
            if (ActorRef.TryParse(value, out key)) return _index.IsValid(key);
            return !string.IsNullOrEmpty(value) && _index.TryFindKey(value, out key);
        }

        /// <summary>
        /// Refreshes the <c>.alive</c> facts of all bound actors. Cheap enough to call about once a second.
        /// </summary>
        public void Sync()
        {
            _updates.Clear();
            foreach (var entry in _service.Blackboard.Entries)
            {
                string name = entry.Key;
                int marker = name.LastIndexOf(ActorMarker, StringComparison.Ordinal);
                if (marker < 0 || !name.StartsWith("ev.", StringComparison.Ordinal)) continue;
                if (name.IndexOf('.', marker + ActorMarker.Length) >= 0) continue; // ".alive" and other derived facts

                bool alive = ActorRef.TryParse(entry.Value.AsString(), out var key) && _index.IsValid(key);
                string aliveKey = $"{name}.{AliveSuffix}";
                if (!_service.Blackboard.TryGet(aliveKey, out var current) || current.AsBool() != alive)
                {
                    _updates.Add(new KeyValuePair<string, bool>(aliveKey, alive));
                }
            }

            foreach (var update in _updates)
            {
                _service.Blackboard.Set(update.Key, update.Value);
            }
        }
    }
}
