using UnityEngine;

namespace Core.Items
{
    [System.Serializable]
    public class CrateInfo
    {
        [SerializeField] private string sourceId;
        [SerializeField] private string crateId;
        [SerializeField] private int capacity;

        public CrateInfo()
        {
        }

        public CrateInfo(string sourceId, string crateId, int capacity)
        {
            this.sourceId = sourceId;
            this.crateId = crateId;
            this.capacity = capacity;
        }
    }
}