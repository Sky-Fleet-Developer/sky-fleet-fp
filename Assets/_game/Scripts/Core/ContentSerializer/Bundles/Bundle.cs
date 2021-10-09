using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Core.ContentSerializer.Bundles
{
    [System.Serializable]
    public class Bundle
    {
        [CanBeNull] public string name = string.Empty;
        [JsonRequired] public int id;
        [JsonRequired] public List<string> tags = new List<string>();

        public Bundle()
        {
        }
    }
}
