using System.Collections.Generic;
using System.Linq;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Wires
{
    [CreateAssetMenu(menuName = "Data/Ports colors data")]
    public class PortsColorsData : SingletonAsset<PortsColorsData>
    {
        public PortTColor[] portTColors;
        public Color powerPortColor;
        public StoragePortColor[] storagePortColors;
        
        
        [System.Serializable]
        public class PortTColor
        {
            public PortType type;
            public Color color;
        }
        
        [System.Serializable]
        public class StoragePortColor
        {
            [ValueDropdown("GetPossibleTypes")]
            public string type;
            private IEnumerable<string> GetPossibleTypes() => Utilities.GetStorageItemNames();
            public Color color;
        }
        
        public Color GetPortColor(Port port)
        {
            if (port is Port<float> f)
            {
                return portTColors.FirstOrDefault(x => x.type == f.ValueType).color;
            }if (port is PowerPort)
            {
                return powerPortColor;
            }if (port is StoragePort stp)
            {
                return storagePortColors.FirstOrDefault(x => x.type == stp.serializedTypeShort).color;
            }
            return Color.white;
        }
    }
}
