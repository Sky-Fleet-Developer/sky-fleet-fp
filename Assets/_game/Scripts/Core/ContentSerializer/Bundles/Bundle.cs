using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.ContentSerializer.Bundles
{
    [System.Serializable]
    public class Bundle
    {
        [JsonRequired] public string name = string.Empty;
        [JsonRequired] public int id;
        [JsonRequired] public List<string> tags = new List<string>();

        public Bundle()
        {
        }
    }
}
