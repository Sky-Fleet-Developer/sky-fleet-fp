using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.Game;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    public class CargoPickUpPointDeliveryService : MonoBehaviour, IProductDeliveryService
    {
        [SerializeField] private Transform spawnAnchor;
        [SerializeField] private Vector2 spawnPlaceSize;
        [SerializeField] private Vector2Int spawnZoneSize;
        [Inject] private ItemsTable _tableItems;
        private int _spawnCounter;
        public int Order => transform.GetSiblingIndex();

        public bool TryDeliver(TradeItem item, ProductDeliverySettings deliverySettings,
            out DeliveredProductInfo deliveredProductInfo)
        {
            if (!item.sign.HasTag(ItemSign.LargeTag))
            {
                deliveredProductInfo = null;
                return false;
            }

            deliveredProductInfo = new DeliveredProductInfo();
            string prefabGuid = _tableItems.GetItemPrefabGuid(item.sign.Id);
            deliveredProductInfo.PrefabLoading = LoadAndInstantiatePrefab(prefabGuid, item, deliverySettings);
            return true;
        }

        private async Task<List<GameObject>> LoadAndInstantiatePrefab(string guid, TradeItem item,
            ProductDeliverySettings deliverySettings)
        {
            var prefab = await TablePrefabs.Instance.GetItem(guid).LoadPrefab();
            List<GameObject> instances = new List<GameObject>(item.amount);
            for (int i = 0; i < item.amount; i++)
            {
                var instance = Instantiate(prefab, spawnAnchor.TransformPoint(GetNextSpawnPoint()), spawnAnchor.rotation);
                instances.Add(instance);
            }
            return instances;
        }

        private Vector3 GetNextSpawnPoint()
        {
            int zone = _spawnCounter++ % (spawnZoneSize.x * spawnZoneSize.y);
            float x = zone % spawnZoneSize.y - spawnZoneSize.x * 0.5f;
            float y = zone / spawnZoneSize.y - spawnZoneSize.y * 0.5f;

            return new Vector3(x * spawnPlaceSize.x, 0, y * spawnZoneSize.y);
        }
    }
}