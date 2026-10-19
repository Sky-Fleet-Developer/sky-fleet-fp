using System.Collections.Generic;
using System.IO;
using Core.ContentSerializer;

namespace Core.World
{
    public class LocationChunkData
    {
        private LinkedList<IWorldEntity> _entities = new ();
        private bool _locked;
        public void Lock()
        {
            _locked = true;
        }

        public void Unlock()
        {
            _locked = false;
        }

        public bool IsEmpty => _entities.Count == 0;
        
        public void AddEntity(IWorldEntity entity)
        {
            if (_locked) return;
            _entities.AddLast(entity);
        }

        public void RemoveEntity(IWorldEntity entity)
        {
            if (_locked) return;
            _entities.Remove(entity);
        }

        public IEnumerable<IWorldEntity> GetEntities()
        {
            var v = _entities.First;
            while (v != null)
            {
                var next = v.Next;
                yield return v.Value;
                v = next;
            }
        }

        public class Serializer : ISerializer<LocationChunkData>
        {
            public void Serialize(LocationChunkData data, Stream stream)
            {
                stream.WriteInt(data._entities.Count);
                var e = data._entities.First;
                while (e != null)
                {
                    var typename = e.Value.GetType().FullName;
                    stream.WriteString(typename);
                    Serializers.GetSerializer(typename).Serialize(e.Value, stream);
                    var components = e.Value.Components;
                    stream.WriteInt(components.Count);
                    foreach (var component in components)
                    {
                        var componentType = component.GetType().FullName;
                        stream.WriteString(componentType);
                        Serializers.GetSerializer(componentType).Serialize(component, stream);
                    }
                    e = e.Next;
                }
            }

            public LocationChunkData Deserialize(Stream stream)
            {
                var data = new LocationChunkData();
                Populate(stream, ref data);
                return data;
            }

            public void Populate(Stream stream, ref LocationChunkData data)
            {
                var entitiesCount = stream.ReadInt();
                for (var i = 0; i < entitiesCount; i++)
                {
                    string typename = stream.ReadString();
                    if (string.IsNullOrEmpty(typename))
                    {
                        continue;
                    }
                    var instance = (IWorldEntity)Serializers.GetSerializer(typename).Deserialize(stream);
                    int componentsCount = stream.ReadInt();
                    for (var c = 0; c < componentsCount; c++)
                    {
                        string componentType = stream.ReadString();
                        instance.Components.Set((IEntityComponent)Serializers.GetSerializer(componentType).Deserialize(stream));
                    }
                    data._entities.AddLast(instance);
                }
            }
        }
    }
}