using System.IO;
using Core.ContentSerializer;
using Core.Misc;

namespace Core.World
{
    /// <summary>
    /// Makes an entity addressable from outside the world (narrative, saves) without knowing its type.
    /// <see cref="Key"/> is issued by the actor index and stays the same between sessions; it is unassigned until then.
    /// <see cref="Alias"/> is a readable name used while authoring.
    /// </summary>
    public class EntityIdentity : IEntityComponent
    {
        public SmKey Key { get; set; }
        public string Alias { get; private set; }
        public bool HasKey => Key.Generation != 0;

        public EntityIdentity()
        {
        }

        public EntityIdentity(string alias)
        {
            Alias = alias;
        }

        public class Serializer : ISerializer<EntityIdentity>
        {
            public void Serialize(EntityIdentity obj, Stream stream)
            {
                stream.WriteInt(obj.Key.Index);
                stream.WriteInt(obj.Key.Generation);
                stream.WriteString(obj.Alias ?? "");
            }

            public void Populate(Stream stream, ref EntityIdentity obj)
            {
                int index = stream.ReadInt();
                int generation = stream.ReadInt();
                obj.Key = new SmKey(index, generation);
                obj.Alias = stream.ReadString();
            }
        }
    }

    public static class EntityAliases
    {
        public static bool IsValidChar(char c) => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '.';

        /// <summary>Lower-cases, trims and replaces characters outside [a-z0-9_.] with '_'. Returns null for an empty result.</summary>
        public static string Normalize(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) return null;
            var chars = alias.Trim().ToLowerInvariant().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (!IsValidChar(chars[i])) chars[i] = '_';
            }

            return new string(chars);
        }
    }
}
