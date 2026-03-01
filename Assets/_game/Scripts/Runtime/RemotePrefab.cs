using System;
using System.Collections.Generic;
using System.Text;
using Core.Configurations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Runtime
{
    public class RemotePrefab : MonoBehaviour, IRemotePrefab
    {
        [FormerlySerializedAs("guid")] [SerializeField] private string assetId;
        [SerializeField] private List<string> tags;
        public string AssetId => assetId;
        public List<string> Tags => tags;

#if UNITY_EDITOR
        public void Reset()
        {
            if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
            {
                var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                var sb = new StringBuilder(name);
                for (int i = 0; i < sb.Length; i++)
                {
                    if (char.IsUpper(sb[i]))
                    {
                        if (i != 0 && sb[i-1] != '_' && sb[i-1] != '-')
                        {
                            sb.Insert(i++, "-");
                        }

                        sb[i] = char.ToLower(sb[i]);
                    }
                }
                assetId = sb.ToString();
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(assetId))
            {
                Reset();
            }
        }
#endif
    }
}