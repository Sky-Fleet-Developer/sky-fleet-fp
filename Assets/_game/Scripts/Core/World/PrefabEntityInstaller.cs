using System;
using Core.Configurations;
using Core.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.World
{
    public class PrefabEntityInstaller : MonoBehaviour
    {
        [SerializeField] private AssetReference prefabReference;

        [Inject] private WorldSpace _worldSpace;

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            Bootstrapper.OnLoadComplete.Subscribe(() =>
            {
                var instance = GetComponent<IRemotePrefab>();
                if (instance != null)
                {
                    _worldSpace.AddEntity(new AssetEntity(instance.transform.gameObject, instance.AssetId));
                    Destroy(this);
                }
                else
                {
                    _worldSpace.AddEntity(new AssetEntity(prefabReference.AssetGUID, transform.position, transform.rotation));
                    Destroy(gameObject);
                }
            });
        }

#if UNITY_EDITOR
        private GameObject _editorInstance;
        private Object _editorInstanceReference;
        private void OnDrawGizmosSelected()
        {
            if (GetComponent<IRemotePrefab>() == null)
            {
                if (!_editorInstance)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        if (transform.GetChild(i).name.Contains(" (DontSave)"))
                        {
                            _editorInstance = transform.GetChild(i).gameObject;
                            _editorInstanceReference = PrefabUtility.GetNearestPrefabInstanceRoot(_editorInstance);
                            break;
                        }
                    }
                }

                if (_editorInstanceReference != prefabReference.editorAsset)
                {
                    if (_editorInstance)
                    {
                        DestroyImmediate(_editorInstance);
                    }

                    if (prefabReference.editorAsset)
                    {
                        _editorInstanceReference = prefabReference.editorAsset;
                        if (_editorInstanceReference is not GameObject)
                        {
                            Debug.LogError("Use PrefabEntityInstaller for prefabs only");
                            return;
                        }
                        _editorInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabReference.editorAsset, transform);
                        _editorInstance.transform.localPosition = Vector3.zero;
                        _editorInstance.transform.localRotation = Quaternion.identity;
                        _editorInstance.hideFlags = HideFlags.DontSave;
                        _editorInstance.name += " (DontSave)";
                        if (_editorInstance.GetComponent<IItemObject>() != null)
                        {
                            Debug.LogError("Dont use PrefabEntityInstaller with item objects. Use ItemObjectInstaller instead.");
                        }
                    }
                }
            }
        }
#endif
    }
}