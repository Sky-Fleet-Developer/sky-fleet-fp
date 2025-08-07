using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Trading
{
    public class DeliveredProductInfo
    {
        public Task<List<GameObject>> PrefabLoading;
        public bool IsPlacedInInventory;
    }
}