using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.Game;
using Core.Items;
using Core.Structure;
using Core.Structure.Rigging.Cargo;
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
            return GetSpawnPoint(zone % spawnZoneSize.x, zone / spawnZoneSize.x);
        }

        private Vector3 GetSpawnPoint(int x, int y)
        {
            return new Vector3((x - (spawnZoneSize.x - 1) * 0.5f) * spawnPlaceSize.x, 0,
                (y - (spawnZoneSize.y - 1) * 0.5f) * spawnPlaceSize.y);
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            for (int i = 0; i < spawnZoneSize.x * spawnZoneSize.y; i++)
            {
                int x = i % spawnZoneSize.x;
                int y = i / spawnZoneSize.x;
                Gizmos.DrawWireCube(GetSpawnPoint(x, y), new Vector3(spawnPlaceSize.x, 0.5f, spawnPlaceSize.y));
            }
            
        }
#endif
    }
}