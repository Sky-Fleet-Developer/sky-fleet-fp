using System;
using System.Collections.Generic;
using System.IO;
using Core.ContentSerializer;
using Core.Misc;
using UnityEngine;

#if FLAT_SPACE
using VectorInt = UnityEngine.Vector2Int;
#else
using VectorInt = UnityEngine.Vector3Int;
#endif

namespace Core.World
{
    /// <summary>What the index knows about an entity, loaded or not.</summary>
    public struct EntityRecord
    {
        public string Alias;
        public string EntityType;
        public VectorInt Cell;
    }

    /// <summary>
    /// Stable addressing of entities that carry an <see cref="EntityIdentity"/>, including those in unloaded chunks.
    /// </summary>
    public interface IEntityIndex
    {
        /// <returns>False if the entity behind the key was destroyed (or the key was never issued).</returns>
        bool IsValid(SmKey key);
        bool TryGetRecord(SmKey key, out EntityRecord record);
        bool TryFindKey(string alias, out SmKey key);
        /// <returns>The entity only while its chunk is loaded.</returns>
        bool TryGetLoadedEntity(SmKey key, out IWorldEntity entity);
    }

    /// <summary>
    /// Used by the world to keep the index in sync with chunks.
    /// </summary>
    public interface IEntityIndexTracker
    {
        /// <summary>Gives the stored index (null if there is none). Placements before this call are queued.</summary>
        void Load(byte[] data);
        void OnPlaced(VectorInt cell, IWorldEntity entity);
        void OnRemoved(IWorldEntity entity);
        /// <summary>Forgets records of the cell whose entity is not in the chunk any more (destroyed).</summary>
        void Reconcile(VectorInt cell, IEnumerable<IWorldEntity> entities);
        /// <returns>False if nothing changed since the last call.</returns>
        bool TrySerialize(out byte[] data);
    }

    public class EntityIndex : IEntityIndex, IEntityIndexTracker
    {
        private const int Magic = 0x58444945; // "EIDX"
        private const int Version = 1;

        private readonly object _lock = new();
        private SlotMap<EntityRecord> _map = new();
        private readonly Dictionary<string, SmKey> _byAlias = new();
        private readonly Dictionary<SmKey, IWorldEntity> _loaded = new();
        private readonly List<(VectorInt cell, IWorldEntity entity)> _pending = new();
        private bool _isLoaded;
        private bool _dirty;

        public bool IsValid(SmKey key)
        {
            lock (_lock) return _map.IsValid(key);
        }

        public bool TryGetRecord(SmKey key, out EntityRecord record)
        {
            lock (_lock) return _map.TryGet(key, out record);
        }

        public bool TryFindKey(string alias, out SmKey key)
        {
            lock (_lock) return _byAlias.TryGetValue(alias, out key);
        }

        public bool TryGetLoadedEntity(SmKey key, out IWorldEntity entity)
        {
            lock (_lock) return _loaded.TryGetValue(key, out entity);
        }

        public void Load(byte[] data)
        {
            lock (_lock)
            {
                _map = new SlotMap<EntityRecord>();
                if (data != null)
                {
                    try
                    {
                        using var stream = new MemoryStream(data);
                        if (stream.ReadInt() != Magic || stream.ReadInt() != Version)
                        {
                            throw new InvalidDataException("Unknown entity index format");
                        }

                        _map = SlotMap<EntityRecord>.Read(stream, ReadRecord);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        _map = new SlotMap<EntityRecord>();
                    }
                }

                _byAlias.Clear();
                _loaded.Clear();
                foreach (var pair in _map)
                {
                    RegisterAlias(pair.Key, pair.Value.Alias);
                }

                _isLoaded = true;
                _dirty = false;
                foreach (var (cell, entity) in _pending)
                {
                    Place(cell, entity);
                }

                _pending.Clear();
            }
        }

        public void OnPlaced(VectorInt cell, IWorldEntity entity)
        {
            lock (_lock)
            {
                if (!_isLoaded)
                {
                    if (entity.Components.TryGet<EntityIdentity>(out _)) _pending.Add((cell, entity));
                    return;
                }

                Place(cell, entity);
            }
        }

        public void OnRemoved(IWorldEntity entity)
        {
            lock (_lock)
            {
                if (!_isLoaded)
                {
                    _pending.RemoveAll(x => x.entity == entity);
                    return;
                }

                if (entity.Components.TryGet<EntityIdentity>(out var identity) && identity.HasKey)
                {
                    _loaded.Remove(identity.Key);
                }
            }
        }

        public void Reconcile(VectorInt cell, IEnumerable<IWorldEntity> entities)
        {
            lock (_lock)
            {
                if (!_isLoaded) return;
                var present = new HashSet<SmKey>();
                foreach (var entity in entities)
                {
                    if (entity.Components.TryGet<EntityIdentity>(out var identity) && identity.HasKey)
                    {
                        present.Add(identity.Key);
                    }
                }

                List<SmKey> destroyed = null;
                foreach (var pair in _map)
                {
                    if (pair.Value.Cell == cell && !present.Contains(pair.Key))
                    {
                        (destroyed ??= new List<SmKey>()).Add(pair.Key);
                    }
                }

                if (destroyed == null) return;
                foreach (var key in destroyed)
                {
                    Forget(key);
                }
            }
        }

        public bool TrySerialize(out byte[] data)
        {
            lock (_lock)
            {
                if (!_isLoaded || !_dirty)
                {
                    data = null;
                    return false;
                }

                using var stream = new MemoryStream();
                stream.WriteInt(Magic);
                stream.WriteInt(Version);
                _map.Write(stream, WriteRecord);
                _dirty = false;
                data = stream.ToArray();
                return true;
            }
        }

        private void Place(VectorInt cell, IWorldEntity entity)
        {
            if (!entity.Components.TryGet<EntityIdentity>(out var identity)) return;
            if (identity.HasKey && _map.IsValid(identity.Key))
            {
                ref var record = ref _map.GetRef(identity.Key);
                if (record.Cell != cell)
                {
                    record.Cell = cell;
                    _dirty = true;
                }

                if (record.Alias != identity.Alias)
                {
                    if (_byAlias.TryGetValue(record.Alias ?? "", out var oldKey) && oldKey == identity.Key) _byAlias.Remove(record.Alias);
                    record.Alias = identity.Alias;
                    RegisterAlias(identity.Key, identity.Alias);
                    _dirty = true;
                }
            }
            else
            {
                if (identity.HasKey)
                {
                    Debug.LogWarning($"Entity key {identity.Key} ('{identity.Alias}') is not in the entity index. A new key is issued, references to the old one are lost.");
                }

                var key = _map.Add(new EntityRecord { Alias = identity.Alias, EntityType = entity.GetType().FullName, Cell = cell });
                identity.Key = key;
                RegisterAlias(key, identity.Alias);
                _dirty = true;
            }

            _loaded[identity.Key] = entity;
        }

        private void RegisterAlias(SmKey key, string alias)
        {
            if (string.IsNullOrEmpty(alias)) return;
            if (_byAlias.TryGetValue(alias, out var existing) && existing != key)
            {
                Debug.LogWarning($"Entity alias '{alias}' is used by several entities ({existing}, {key}); '{alias}' resolves to the first.");
                return;
            }

            _byAlias[alias] = key;
        }

        private void Forget(SmKey key)
        {
            if (!_map.TryGet(key, out var record)) return;
            if (!string.IsNullOrEmpty(record.Alias) && _byAlias.TryGetValue(record.Alias, out var aliasKey) && aliasKey == key)
            {
                _byAlias.Remove(record.Alias);
            }

            _loaded.Remove(key);
            _map.Remove(key);
            _dirty = true;
        }

        private static void WriteRecord(Stream stream, EntityRecord record)
        {
            stream.WriteString(record.Alias ?? "");
            stream.WriteString(record.EntityType ?? "");
            stream.WriteInt(record.Cell.x);
            stream.WriteInt(record.Cell.y);
#if !FLAT_SPACE
            stream.WriteInt(record.Cell.z);
#endif
        }

        private static EntityRecord ReadRecord(Stream stream)
        {
            var record = new EntityRecord
            {
                Alias = stream.ReadString(),
                EntityType = stream.ReadString()
            };
            record.Cell.x = stream.ReadInt();
            record.Cell.y = stream.ReadInt();
#if !FLAT_SPACE
            record.Cell.z = stream.ReadInt();
#endif
            return record;
        }
    }
}
