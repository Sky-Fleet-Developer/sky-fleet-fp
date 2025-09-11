using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Core.Items
{
    [CreateAssetMenu(menuName = "Factories/ItemFactory", fileName = "ItemFactory")]
    public class ItemFactory : ScriptableObject
    {
        [Inject] private TablePrefabs _tablePrefabs;
        [Inject] private ItemsTable _tableItems;

        public async Task<List<GameObject>> ConstructItem(TradeItem item)
        {
            var prefab = await _tablePrefabs.GetItem(_tableItems.GetItemPrefabGuid(item.sign.Id)).LoadPrefab();
            List<GameObject> instances = new List<GameObject>((int)item.amount);
            foreach (var makeInstance in item.MakeInstances())
            {
                var instance = ConstructItemPrivate(makeInstance, prefab);
                instances.Add(instance);
            }

            return instances;
        }

        public async Task<GameObject> ConstructItem(ItemInstance item)
        {
            var prefab = await _tablePrefabs.GetItem(_tableItems.GetItemPrefabGuid(item.Sign.Id)).LoadPrefab();
            return ConstructItemPrivate(item, prefab);
        }

        private GameObject ConstructItemPrivate(ItemInstance item, GameObject source)
        {
            var instance = Instantiate(source);
            if (instance.TryGetComponent(out IItemObjectHandle itemObjectHandle))
            {
                itemObjectHandle.SetSourceItem(item);
            }

            return instance;
        }
    }
}