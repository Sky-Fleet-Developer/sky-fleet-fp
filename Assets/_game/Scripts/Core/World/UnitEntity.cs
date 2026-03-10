using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Ai;
using Core.ContentSerializer;
using Core.Data;
using Core.Game;
using Core.Items;
using Core.Structure;
using Core.Structure.Serialization;
using Core.Utilities;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public class UnitEntity : ItemEntity
    {
        private IUnit _unit;
        public IUnit Unit => _unit;
        
        public UnitEntity() : base()
        {
        }

        public UnitEntity(ItemDescription itemDescription, Vector3 position, Quaternion rotation) : base(itemDescription, position, rotation)
        {
        }
        
        public UnitEntity(IUnit unit, IItemObject objectInstance, ItemDescription itemDescription) : base(objectInstance, itemDescription)
        {
            _unit = unit;
        }

        protected override void OnSpawn(IItemObject instance)
        {
            base.OnSpawn(instance);
            _unit = GameObject.GetComponent<IUnit>();
        }

        public override string ToString()
        {
            if (_unit is Component component && component)
            {
                return $"ActorEntity: {component.transform.name}";
            }
            return "Actor: null instance";
        }
        
        public class Serializer : ISerializer<UnitEntity>
        {
            private static readonly ISerializer BaseSerializer = Serializers.GetSerializer(typeof(ItemEntity));

            public void Serialize(UnitEntity obj, Stream stream)
            {
                BaseSerializer.Serialize(obj, stream);
            }

            public void Populate(Stream stream, ref UnitEntity obj)
            {
                var entity = new UnitEntity();
                var boxed = (object)entity;
                BaseSerializer.Populate(stream, ref boxed);
            }
        }
    }
}