using System;
using System.Collections.Generic;
using System.Linq;
using Core.Data;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Configurations
{
    [CreateAssetMenu(menuName = "Configs/VolumePrefabsProcessor")]
    public class PrefabVolumeProcessor : PrefabProcessor
    {
        [Serializable]
        public class PrefabVolumeProfile
        {
            #if UNITY_EDITOR
            [ShowInInspector]
            private GameObject prefab
            {
                get
                {
                    return TablePrefabs.Instance.GetItem(prefabGuid).GetReferenceInEditor();
                }
            }
            #endif
            [SerializeField] private string prefabGuid;
            [SerializeField] private List<Vector3Int> volume;
            [SerializeField] private Bounds bounds;
            public string PrefabGuid => prefabGuid;

            public PrefabVolumeProfile()
            {
            }

            public PrefabVolumeProfile(string guid)
            {
                prefabGuid = guid;
            }

            public IReadOnlyList<Vector3Int> GetVolume() => volume;

            public void SetVolume(List<Vector3Int> value)
            {
                volume = value;
            }

            public void SetBounds(Bounds value)
            {
                bounds = value;
            }

            public Bounds GetBounds()
            {
                return bounds;
            }
        }

        [SerializeField] private float particleSize;
        [SerializeField] private List<PrefabVolumeProfile> profiles;
        private Dictionary<string, PrefabVolumeProfile> _profilesByGuid;
        public float ParticleSize => particleSize;

        protected override async void Process(RemotePrefabItem item)
        {
            PrefabVolumeProfile profile = profiles.FirstOrDefault(x => x.PrefabGuid == item.guid);
            if (profile == null)
            {
                profile = new PrefabVolumeProfile(item.guid);
                profiles.Add(profile);
            }

            Transform target = null;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                target = (await item.LoadPrefab()).transform;
            }
            else
            {
                target = item.GetReferenceInEditor().transform;
            }
#else
            target = (await item.LoadPrefab()).transform;
#endif
            SetupProfile(profile, target);
        }

        private readonly Vector3 spawnPoint = Vector3.down * 700;
        private void SetupProfile(PrefabVolumeProfile profile, Transform target)
        {
            Transform instance;
            Vector3 setupPosition = Vector3.zero;
            Quaternion setupRotation = Quaternion.identity;
            if (target.gameObject.activeInHierarchy)
            {
                instance = target;
                setupPosition = instance.position;
                setupRotation = instance.rotation;
                instance.position = spawnPoint;
                instance.rotation = Quaternion.identity;
            }
            else
            {
                instance = Instantiate(target, spawnPoint, Quaternion.identity, null);
            }

            List<Vector3Int> volume = new();

            Bounds bounds = instance.GetBounds();

            Vector3Int startPoint = GetCellForPoint(bounds.min);
            Vector3Int endPoint = GetCellForPoint(bounds.max);
            Vector3 halfExtents = Vector3.one * (particleSize * 0.5f);
            for(int x = startPoint.x; x <= endPoint.x; x++)
            {
                for(int y = startPoint.y; y <= endPoint.y; y++)
                {
                    for(int z = startPoint.z; z <= endPoint.z; z++)
                    {
                        var current = new Vector3Int(x, y, z);

                        bool result = Physics.CheckBox(spawnPoint + (Vector3)current * particleSize, halfExtents);
                        if (result)
                        {
                            volume.Add(current);
                        }
                    }
                }
            }
            profile.SetVolume(volume);
            profile.SetBounds(bounds);

            if (!target.gameObject.activeInHierarchy)
            {
                if (Application.isPlaying)
                {
                    Destroy(instance.gameObject);
                }
                else
                {
                    DestroyImmediate(instance.gameObject);
                }
            }
            else
            {
                instance.position = setupPosition;
                instance.rotation = setupRotation;
            }
        }

        public Vector3Int GetCellForPoint(Vector3 point)
        {
            return new Vector3Int(Mathf.RoundToInt(point.x / particleSize), Mathf.RoundToInt(point.y / particleSize),
                Mathf.RoundToInt(point.z / particleSize));
        }

        public PrefabVolumeProfile GetProfile(ITablePrefab tablePrefab)
        {
            _profilesByGuid ??= profiles.ToDictionary(x => x.PrefabGuid);

            if (_profilesByGuid.TryGetValue(tablePrefab.Guid, out var profile))
            {
                return profile;
            }
            else
            {
                profile = new PrefabVolumeProfile(tablePrefab.Guid);
                profiles.Add(profile);
                _profilesByGuid.Add(tablePrefab.Guid, profile);
                
                SetupProfile(profile, tablePrefab.transform);
                return profile;
            }
        }
    }
}