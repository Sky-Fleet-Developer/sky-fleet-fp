using Core.Character.Stuff;
using Core.Items;
using Core.Trading;
using Core.UIStructure.Utilities;
using Core.Utilities;
using UnityEngine;

namespace Runtime.Trading.UI
{
    public class SlotCellView : MonoBehaviour, IDragAndDropContainer, IInventoryStateListener
    {
        [SerializeField] private ThingView<ItemInstance> thingView;
        private SlotCell _cell;
        public string SlotKey => _slotKey;
        private string _slotKey;
        private ThingsListView<ItemInstance> _thingsListViewSource;
        private ThingsListView<ItemInstance> _thingsListView;
        private Transform _thingsListContainer;

        public SlotCell Cell => _cell;

        public void Init(string slotKey, ThingsListView<ItemInstance> thingsListViewSource, Transform thingsListContainer)
        {
            _thingsListContainer = thingsListContainer;
            _thingsListViewSource = thingsListViewSource;
            _slotKey = slotKey;
            thingView.SetContainer(this);
        }
        
        public void Set(SlotCell cell)
        {
            RemoveCell();
            
            _cell = cell;
            if (_cell.IsContainer)
            {
                if (!_thingsListView)
                {
                    _thingsListView = DynamicPool.Instance.Get(_thingsListViewSource, _thingsListContainer);
                }
                _cell.AddListener(this);
            }
            else if (_thingsListView)
            {
                DynamicPool.Instance.Return(_thingsListView);
            }
            Refresh();
        }

        public void Refresh()
        {
            if (_cell.Item != null)
            {
                thingView.gameObject.SetActive(true);
                thingView.SetData(_cell.Item);
            }
            else
            {
                thingView.gameObject.SetActive(false);
            }
        }
        
        public void OnDropContent(DropEventData eventData)
        {
            
        }

        public void ItemAdded(ItemInstance item)
        {
            _thingsListView.AddItem(item);
        }

        public void ItemMutated(ItemInstance item)
        {
            _thingsListView.RefreshItem(item);
        }

        public void ItemRemoved(ItemInstance item)
        {
            _thingsListView.RemoveItem(item);
        }

        private void RemoveCell()
        {
            if (_cell != null && _thingsListView)
            {
                _cell.RemoveListener(this);
            }
            _cell = null;
        }
    }
}