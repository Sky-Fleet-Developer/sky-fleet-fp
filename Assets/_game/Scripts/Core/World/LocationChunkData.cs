using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Core.Utilities;
using Unity.Properties;
using UnityEngine;

namespace Core.World
{
    public class LocationChunkData
    {
        private LinkedList<IWorldEntity> _entities = new LinkedList<IWorldEntity>();
        
        public async Task Serialize(FileStream stream)
        {
            stream.WriteInt(_entities.Count);
            var e = _entities.First;
            List<Task> tasks = new List<Task>(_entities.Count);
            while (e != null)
            {
                stream.WriteString(e.Value.GetType().FullName);
                tasks.Add(e.Value.Serialize(stream));
                e = e.Next;
            }
            
            await Task.WhenAll(tasks);
        }

        public async Task Deserialize(FileStream stream)
        {
            var entitiesCount = stream.ReadInt();
            List<Task> tasks = new List<Task>(entitiesCount);
            for (var i = 0; i < entitiesCount; i++)
            {
                string structureType = stream.ReadString();
                if (string.IsNullOrEmpty(structureType))
                {
                    continue;
                }
                Type type = TypeExtensions.GetTypeByName(structureType);
                if (Activator.CreateInstance(type) is IWorldEntity instance)
                {
                    _entities.AddLast(instance);
                    tasks.Add(instance.Deserialize(stream));
                }
            }
            
            await Task.WhenAll(tasks);
        }

        public void AddEntity(IWorldEntity entity)
        {
            _entities.AddLast(entity);
        }

        public void RemoveEntity(IWorldEntity entity)
        {
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
    }
}