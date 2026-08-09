using System.Collections.Generic;
using Core.Ai;
using Core.Structure.Damage;
using Core.World;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Core.Items
{
    
    [ExecuteInEditMode, RequireComponent(typeof(IItemObject))]
    public partial class EntityObjectInstaller : MonoBehaviour
    {
        [HideIf("distributedStructure")] public ItemDescription itemDescription = new ();
        [ShowIf("distributedStructure"), ShowInInspector]
        private List<ItemDescription> ChildDescriptions
        {
            get => itemDescription.nestedItems;
            set => itemDescription.nestedItems = value;
        }
        [SerializeField] private bool distributedStructure;
        [Inject] private WorldSpace _worldSpace;
        

        static EntityObjectInstaller()
        {
            StructureDamageProfileHub.SetupDamageProfileCreationAction(typeof(EntityObjectInstaller), Destroy);
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            Bootstrapper.OnLoadComplete.Subscribe(() =>
            {
                if (!_worldSpace)
                {
                    return;
                }
                var itemObject = GetComponent<IItemObject>();
                if (itemObject != null)
                {
                    var unit = GetComponent<IUnit>();
                    if (unit != null)
                    {
                        _worldSpace.AddEntity(new UnitEntity(unit, itemObject, itemDescription));
                    }
                    else
                    {
                        _worldSpace.AddEntity(new ItemEntity(itemObject, itemDescription));
                    }
                }
                Destroy(this);
            });
        }
    }
}
