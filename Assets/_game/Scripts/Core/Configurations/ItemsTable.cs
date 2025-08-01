using Core.Configurations.GoogleSheets;
using Core.Trading;
using UnityEngine;

namespace Core.Configurations
{
    [CreateAssetMenu(menuName = "Configs/Items")]
    public class ItemsTable : Table<ItemSign>
    {
        [SerializeField] private ItemSign[] items;
        public override string TableName => "Items";

        public override ItemSign[] Data
        {
            get => items;
            protected set
            {
                items = value;
            }
        }
    }
}