using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Ai;
using Core.ContentSerializer;
using Core.Data;
using Core.Game;
using Core.Items;
using Core.Misc;
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
    public class UnitEntity : ItemEntity, ISignatureData
    {
        private IUnit _unit;
        private IUnitTactic _tactic;
        private string _signature;
        private string _cachedName;
        public IUnit Unit => _unit;

        public Vector3 Velocity => Rigidbody?.linearVelocity ?? Vector3.zero;
        public string SignatureId => _signature;
        public UnitTechCharacteristic GetTechCharacteristic() => _unit?.GetTechCharacteristic() ?? default;

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

        [Inject]
        private void Inject(DiContainer container)
        {
            OverrideContainer = container.CreateSubContainer();
            OverrideContainer.Bind<UnitEntity>().FromInstance(this);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (!string.IsNullOrEmpty(_signature))
            {
                ItemInstance.SetSignatureProperty(_signature);
            }
            else
            {
                RefreshSignature();
            }
        }

        public void SetSignature(string value)
        {
            _signature = value;
            ItemInstance?.SetSignatureProperty(value);
        }
        
        public void SetAiActivity(bool isActive)
        {
            ((MonoBehaviour)_unit).enabled = isActive;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            _unit = GameObject.GetComponent<IUnit>();
            if (_unit is Component component && component)
            {
                _cachedName = $"UnitEntity ({Id}): {component.transform.name}";
            }
            _cachedName = $"Unit ({Id}): {_unit}";
            SetupUnit();
        }

        private void RefreshSignature()
        {
            if (ItemInstance.TryGetProperty(Property.SignatureIdPropertyName, out var property))
            {
                _signature = property.values[Property.SignatureId_Signature].stringValue;
            }
        }

        private void SetupUnit()
        {
            _unit.SetTactic(_tactic);
            _unit.InjectEntity(this);
        }

        public override string ToString()
        {
            return _cachedName;
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

        public void SetTactic(IUnitTactic tactic)
        {
            _tactic = tactic;
            _unit?.SetTactic(tactic);
        }
        
        public IUnitTactic GetTactic() => _tactic;
    }
}