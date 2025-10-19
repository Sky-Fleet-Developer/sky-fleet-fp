using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Core.Utilities;
using Unity.Properties;

namespace Core.World
{
    public class LocationChunkData
    {
        public List<IWorldEntity> Entities;
        
        public async Task Serialize(FileStream stream)
        {
            
        }

        public async Task Deserialize(FileStream stream)
        {
            var entitiesCount = stream.ReadInt();
            Entities = new List<IWorldEntity>(entitiesCount);
            for (var i = 0; i < entitiesCount; i++)
            {
                string structureType = stream.ReadString();
                Type type = TypeExtensions.GetTypeByName(structureType);
                if (Activator.CreateInstance(type) is IWorldEntity instance)
                {
                    Entities[i] = instance;
                }
            }
        }
    }
}