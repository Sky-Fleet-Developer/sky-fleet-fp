using Core.Items;
using Core.Trading;
using Core.World;
using UnityEngine;

namespace Runtime.Items
{
    [RequireComponent(typeof(Container), typeof (DynamicWorldObject))]
    public class ContainerItemMass : MonoBehaviour, IInventoryStateListener, IMassModifier
    {
        private DynamicWorldObject _dynamicWorldObject;
        private Container _container;
        private float _mass;
        private IMassCombinator _massCombinator;
        public float Mass => _mass;

        private void Awake()
        {
            _dynamicWorldObject = GetComponent<DynamicWorldObject>();
            _container = GetComponent<Container>();
            _container.AddListener(this);
        }

        private void OnItemInit()
        {
            RefreshMass();
        }

        private void RefreshMass()
        {
            _mass = _container.GetMass();
            _massCombinator?.SetMassDirty(this);
        }

        public void ItemAdded(ItemInstance item)
        {
            OnMassDirty();
        }

        public void ItemMutated(ItemInstance item)
        {
            OnMassDirty();
        }

        public void ItemRemoved(ItemInstance item)
        {
            OnMassDirty();
        }

        private void OnMassDirty()
        {
            RefreshMass();
        }

        public void AddListener(IMassCombinator massCombinator)
        {
            _massCombinator = massCombinator;
        }

        public void RemoveListener(IMassCombinator massCombinator)
        {
            if (_massCombinator == massCombinator)
            {
                _massCombinator = null;
            }
        }
    }
}