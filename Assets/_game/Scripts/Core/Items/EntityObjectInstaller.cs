using Core.Ai;
using Core.Structure;
using Core.World;
using UnityEngine;
using Zenject;

namespace Core.Items
{
    
    [ExecuteInEditMode, RequireComponent(typeof(IItemObject))]
    public partial class EntityObjectInstaller : MonoBehaviour
    {
        public ItemDescription itemDescription = new ();
        [Inject] private WorldSpace _worldSpace;
            
        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            Bootstrapper.OnLoadComplete.Subscribe(() =>
            {
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
