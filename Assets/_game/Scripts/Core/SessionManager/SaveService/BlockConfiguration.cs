using System.Collections.Generic;
using System.Reflection;
using Core.Structure;
using Core.Structure.Rigging;
using UnityEngine;

namespace Core.SessionManager.SaveService
{
    [System.Serializable]
    public class BlockConfiguration
    {
        public string path; //путь к модолю по трансформам/парент
        public string blockName;
        public int sibilingIdx;
        public string currentGuid; // текущий гуид
        public Vector3 localPosition;
        public Vector3 localRotation;
        
        private Dictionary<string, string> setup;//свойства помеченные [PlayerProperty]
        [SerializeField] private List<string> setupKeys = new List<string>();
        [SerializeField] private List<string> setupValues = new List<string>();

        public BlockConfiguration(IBlock block, string path)
        {
            this.path = path;
            blockName = block.transform.name;
            sibilingIdx = block.transform.GetSiblingIndex();
            currentGuid = block.Guid;
            localPosition = block.transform.localPosition;
            localRotation = block.transform.localEulerAngles;
            PropertyInfo[] properties = Factory.GetBlockProperties(block);

            for (int i = 0; i < properties.Length; i++)
            {
                string value = properties[i].GetValue(block).ToString();
                AddSetup(properties[i].Name, value);
            }
        }

        public void AddSetup(string key, string value)
        {
            setupKeys.Add(key);
            setupValues.Add(value);
            setup?.Add(key, value);
        }

        public bool TryGetSetup(string key, out string value)
        {
            if (setup == null)
            {
                setup = new Dictionary<string, string>();
                for (var i = 0; i < setupKeys.Count; i++)
                {
                    setup.Add(setupKeys[i], setupValues[i]);
                }
            }

            return setup.TryGetValue(key, out value);
        }

        public void ApplyPrimarySetup(IBlock block)
        {
            Transform transform = block.transform;
            transform.localPosition = localPosition;
            transform.localEulerAngles = localRotation;
            transform.localScale = Vector3.one;
            transform.name = blockName;
            transform.SetSiblingIndex(sibilingIdx);
        }
    }
}