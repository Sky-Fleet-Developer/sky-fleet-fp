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
                    _worldSpace.AddEntity(new ItemEntity(itemObject, itemDescription));
                }
                Destroy(this);
            });
        }
    }
}
