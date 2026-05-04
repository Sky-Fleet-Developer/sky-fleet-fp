using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Items;
using Core.Trading;
using Core.Weapon;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Core.Structure.Damage
{
    public class StructureDamageProfileHub : MonoBehaviour, IMyInstaller
    {
        private struct StructureArchetype : IEquatable<StructureArchetype>
        {
            public string Body;
            public int BlocksHash;

            public StructureArchetype(IStructure structure, BankSystem bankSystem)
            {
                Body = structure.AssetId;
                int blocksHash = 0;
                var container = bankSystem.GetOrCreateInventory(structure.SourceItem.ContainerKey);
                foreach (var item in container.GetItems())
                {
                    if (item.Sign.HasTag(ItemSign.BlockTag))
                    {
                        blocksHash ^= item.Sign.Id.GetHashCode();
                    }
                }
                BlocksHash = blocksHash;
            }

            public bool Equals(StructureArchetype other)
            {
                return Body == other.Body && BlocksHash == other.BlocksHash;
            }

            public override bool Equals(object obj)
            {
                return obj is StructureArchetype other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Body, BlocksHash);
            }
        }
        
        [Inject] private StructureUpdateSystem _structureUpdateSystem;
        [Inject] private BankSystem _bankSystem;
        [Inject] private IItemObjectFactory _itemObjectFactory;
        private Dictionary<StructureArchetype, StructureDamageModel> _profiles = new();
        //private Dictionary<IStructure, StructureArchetype> _archetypeByStructure = new();

        private void Start()
        {
            _structureUpdateSystem.OnStructureAdd += StructureAdded;
            foreach (var structure in _structureUpdateSystem.Structures())
            {
                StructureAdded(structure);
            }
            //_structureUpdateSystem.OnStructureRemoved += StructureRemoved;
        }

        private void StructureAdded(IStructure structure)
        {
            structure.OnInitComplete.Subscribe(() =>
            {
                var archetype = new StructureArchetype(structure, _bankSystem);
                //_archetypeByStructure.Add(structure, archetype);
                if (!_profiles.ContainsKey(archetype))
                {
                    var profile = CreateProfile(structure);
                    _profiles.Add(archetype, profile);
                }
            });
        }

        //private void StructureRemoved(IStructure structure)
        //{
        //    _archetypeByStructure.Remove(structure);
        //}

        private StructureDamageModel CreateProfile(IStructure structure)
        {
            var model = new StructureDamageModel();
            SetupModel(model, structure).Forget();
            return model;
        }

        private async UniTaskVoid SetupModel(StructureDamageModel model, IStructure structure)
        {
            IItemObject colliderInstance = await _itemObjectFactory.CreateSingle(structure.SourceItem, true);
            colliderInstance.transform.SetParent(transform);
            colliderInstance.transform.localPosition = Vector3.zero;
            colliderInstance.transform.localRotation = Quaternion.identity;
            if (colliderInstance.transform.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.isKinematic = true;
            }
            
            //IDamagable[] damagable = colliderInstance.transform.GetComponentsInChildren<IDamagable>();
            model.Root = colliderInstance.transform;
            model.Parents = structure.Parents.Select(x => x.Transform).ToArray();
            
            StructureDamageModelLink.CreateForStructure(structure, model);
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<StructureDamageProfileHub>().FromInstance(this).AsSingle();
        }
    }
}