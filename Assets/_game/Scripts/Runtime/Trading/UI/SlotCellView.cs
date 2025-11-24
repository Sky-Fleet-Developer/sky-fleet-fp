using System;
using System.Collections;
using System.Collections.Generic;
using Core.Character.Interface;
using Core.Character.Stuff;
using Core.Items;
using Core.Trading;
using Core.UIStructure.Utilities;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Runtime.Trading.UI
{
    public class SlotCellView : MonoBehaviour, IDragAndDropContainer, IInventoryStateListener, IDragCallbacks<DraggableThingView<ItemInstance>>
    {
        [SerializeField] private DraggableThingView<ItemInstance> thingView;
        [Inject] private DragAndDropService _dragAndDropService;
        [Inject] private DragAndDropItemsMediator _dragAndDropItemsMediator;
        [Inject] private DiContainer _diContainer;
        private SlotCell _cell;
        public string SlotKey => _slotKey;
        private string _slotKey;
        private SlotContainerListView _thingsListViewSource;
        private SlotContainerListView _thingsListView;
        private Transform _thingsListContainer;
        private IPullPutItem _itemsSource;
        private Vector2 _dragPosition;
        private DraggableThingView<ItemInstance>[] _draggableItemAsArray = new DraggableThingView<ItemInstance>[1];
        private bool _isDnDRegistered = false;
        public SlotCell Cell => _cell;

        public void Init(string slotKey, SlotContainerListView thingsListViewSource, Transform thingsListContainer, IPullPutItem itemsSource)
        {
            _itemsSource = itemsSource;
            _thingsListContainer = thingsListContainer;
            _thingsListViewSource = thingsListViewSource;
            _slotKey = slotKey;
            thingView.SetContainer(this);
            thingView.SetDragCallbacks(this);
            _draggableItemAsArray[0] = thingView;
            EnsureDnDRegistered();
        }

        private void EnsureDnDRegistered()
        {
            if (!_isDnDRegistered)
            {
                _dragAndDropItemsMediator.RegisterContainerView(this, _itemsSource);
                _isDnDRegistered = true;
            }
        }
        
        private void OnEnable()
        {
            if (_dragAndDropItemsMediator == null)
            {
                return;
            }
            EnsureDnDRegistered();
        }

        private void OnDisable()
        {
            _isDnDRegistered = false;
            _dragAndDropItemsMediator.UnregisterContainerView(this);
        }

        /*private ItemInstance TryPullItem(ItemInstance item, float amount)
        {
            if(_itemsSource.TryPullItem(item, amount, out var result))
            {
                return result;
            }
            return null;
        }

        private bool TryPutItem(ItemInstance item)
        {
            return _itemsSource.TryPutItem(item);
        }*/

        public void Set(SlotCell cell)
        {
            RemoveCell();
            
            _cell = cell;
            if (_cell.IsContainer)
            {
                if (!_thingsListView)
                {
                    _thingsListView = DynamicPool.Instance.Get(_thingsListViewSource, _thingsListContainer);
                    _dragAndDropItemsMediator.RegisterContainerView(_thingsListView, _cell);
                    _thingsListView.OnDropContentEvent += OnDropContentToThingsList;
                    _diContainer.Inject(_thingsListView);
                }
                _thingsListView.Init(cell);
                _cell.AddListener(this);
            }
            else if (_thingsListView)
            {
                _dragAndDropItemsMediator.UnregisterContainerView(_thingsListView);
                _thingsListView.OnDropContentEvent -= OnDropContentToThingsList;
                DynamicPool.Instance.Return(_thingsListView);
                _thingsListView = null;
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

        private void OnDropContentToThingsList(DropEventData eventData)
        {
            _dragAndDropItemsMediator.DragAndDropPreformed(eventData, _thingsListView);
        }

        public void OnDropContent(DropEventData eventData)
        {
            _dragAndDropItemsMediator.DragAndDropPreformed(eventData, this);
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

        public void OnChildDragStart(DraggableThingView<ItemInstance> view, Vector2 position)
        {
            _dragPosition = position;
            if (view.IsSelected)
            {
                _dragAndDropService.BeginDrag(position, this, _draggableItemAsArray);
            }
            else
            {
                _dragAndDropService.BeginDrag(position, view);
            }
        }
        
        public void OnChildDragEnd(DraggableThingView<ItemInstance> view)
        {
            _dragAndDropService.Drop();
        }

        public void OnChildDragContinue(DraggableThingView<ItemInstance> view, Vector2 delta)
        {
            _dragPosition += delta;
            _dragAndDropService.Move(_dragPosition);
        }

        public void Clear()
        {
            RemoveCell();
        }
    }
}