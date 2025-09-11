using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.Data;
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
        [Inject] private ItemFactory _itemFactory;
        [Inject] private TablePrefabs _tablePrefabs;
        private int _spawnCounter;
        public int Order => transform.GetSiblingIndex();

        public bool TryDeliver(TradeItem item, ProductDeliverySettings deliverySettings,
            out DeliveredProductInfo deliveredProductInfo)
        {
            if (item.sign.HasTag(ItemSign.LiquidTag))
            {
                deliveredProductInfo = null;
                return false;
            }

            deliveredProductInfo = new DeliveredProductInfo();
            deliveredProductInfo.PrefabLoading = LoadAndInstantiatePrefab(item, deliverySettings);
            return true;
        }

        private async Task<List<GameObject>> LoadAndInstantiatePrefab(TradeItem item,
            ProductDeliverySettings deliverySettings)
        {
            var instances = await _itemFactory.ConstructItem(item);
            for (var i = 0; i < instances.Count; i++)
            {
                instances[i].transform.position = spawnAnchor.TransformPoint(GetNextSpawnPoint());
                instances[i].transform.rotation = spawnAnchor.rotation;
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