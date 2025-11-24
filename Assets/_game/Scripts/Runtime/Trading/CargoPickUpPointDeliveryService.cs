using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.Data;
using Core.Game;
using Core.Items;
using Core.Localization;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    public class CargoPickUpPointDeliveryService : MonoBehaviour, IItemDeliveryService
    {
        [SerializeField] private Transform spawnAnchor;
        [SerializeField] private Vector2 spawnPlaceSize;
        [SerializeField] private Vector2Int spawnZoneSize;
        [Inject(Optional = true)] private IItemObjectFactory _iItemObjectFactory;
        [Inject(Optional = true)] private TablePrefabs _tablePrefabs;
        private int _spawnCounter;
        public int Order => transform.GetSiblingIndex();

        public PutItemResult Deliver(ItemInstance item, IInventoryOwner destination)
        {
            if(!IsCanDeliver(item.Sign, destination)) return PutItemResult.Fail;
            LoadAndInstantiatePrefab(item, destination);
            return PutItemResult.Fully;
        }

        public bool IsCanDeliver(ItemSign item, IInventoryOwner destination)
        {
            return !item.HasTag(ItemSign.LiquidTag);
        }

        private async void LoadAndInstantiatePrefab(ItemInstance item, IInventoryOwner destination)
        {
            var instances = await _iItemObjectFactory.Create(item);
            for (var i = 0; i < instances.Count; i++)
            {
                instances[i].transform.position = spawnAnchor.TransformPoint(GetNextSpawnPoint());
                instances[i].transform.rotation = spawnAnchor.rotation;
            }
            //return instances;
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
        public string NameToView => LocalizationService.Localize($"cargo-zone-delivery_name");
        public string IconKey => "ui_cargo-zone-delivery_icon";
    }
}