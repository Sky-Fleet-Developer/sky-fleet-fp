using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Items;
using Core.Trading;
using Core.Utilities;
using Core.Weapon;
using Core.World;
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
        [Inject] private ProjectileHandler _projectileHandler;
        private Dictionary<StructureArchetype, StructureDamageModelPool> _profiles = new();

        private static Dictionary<Type, (Action<Component> action, int order)> setupActions = new();
        public static void SetupDamageProfileCreationAction(Type type, Action<Component> action, int order = 0) => setupActions[type] = (action, order);

        private Vector3 _offset = Vector3.left * 200;
        private float _maxBoundInRow;
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
                if (!_profiles.TryGetValue(archetype, out var pool))
                {
                    var profile = CreateProfile(structure);
                    _profiles.Add(archetype, profile);
                }
                else
                {
                    StructureDamageModelLink.CreateForStructure(structure, pool);
                }
                structure.transform.InitAsStructurePart(_projectileHandler);
            });
        }

        private StructureDamageModelPool CreateProfile(IStructure structure)
        {
            var pool = new StructureDamageModelPool(4, _projectileHandler);
            SetupModel(pool, structure).Forget();
            return pool;
        }

        private async UniTaskVoid SetupModel(StructureDamageModelPool pool, IStructure structure)
        {
            IItemObject colliderInstance;
            if (structure.SourceItem.Sign.Id.Equals(ItemSign.Unknown))
            {
                colliderInstance = Instantiate(structure.transform).GetComponent<IItemObject>();
                _itemObjectFactory.SetupInstance((IItemObjectHandle)colliderInstance, structure.SourceItem, true);
            }
            else
            {
                colliderInstance = await _itemObjectFactory.CreateSingle(structure.SourceItem, true);
            }
            colliderInstance.transform.name = $"{structure.transform.name}_colliderReference_(0)";
            colliderInstance.transform.SetParent(transform);
            colliderInstance.transform.localPosition = _offset;
            colliderInstance.transform.localRotation = Quaternion.identity;
            if (colliderInstance.transform.TryGetComponent(out WorldOffsetAnchor anchor))
            {
                Destroy(anchor);
            }
            var boundsSize = colliderInstance.transform.GetBounds().size; 
            _offset.x += boundsSize.x * 3;
            _maxBoundInRow = Mathf.Max(_maxBoundInRow, boundsSize.z);
            if (_offset.x > 200)
            {
                _offset.x = -_offset.x;
                _offset.z += _maxBoundInRow * 3;
            }
            if (colliderInstance.transform.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.isKinematic = true;
            }
            
            foreach (var v in colliderInstance.transform.GetComponentsInChildren<Component>()
                         .Where(x => setupActions.ContainsKey(x.GetType()))
                         .Select(x => (setupActions[x.GetType()], x))
                         .OrderBy(x => x.Item1.order))
            {
                v.Item1.action(v.Item2);
            }
            
            //IDamagable[] damagable = colliderInstance.transform.GetComponentsInChildren<IDamagable>();
            pool.Init((IStructure)colliderInstance, colliderInstance.transform.localPosition, Vector3.up * (boundsSize.y * 3));
            
            StructureDamageModelLink.CreateForStructure(structure, pool);
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<StructureDamageProfileHub>().FromInstance(this).AsSingle();
        }
    }
}