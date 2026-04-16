using Core.Items;
using Core.Utilities;
using Zenject;

namespace Runtime.Items
{
    public class ItemObject : RemotePrefab, IItemObjectHandle
    {
        [Inject] private IItemObjectFactory _itemObjectFactory;
        private ItemInstance _sourceItem;
        public ItemInstance SourceItem => _sourceItem;
        public LateEvent OnItemInitialized = new ();

        void IItemObjectHandle.SetSourceItem(ItemInstance sign)
        {
            _sourceItem = sign;
            OnItemSet();
            OnItemInitialized.Invoke();
        }

        protected virtual void OnItemSet()
        {
        }

        public void Deconstruct()
        {
            _itemObjectFactory.Deconstruct(this);
        }
    }
}