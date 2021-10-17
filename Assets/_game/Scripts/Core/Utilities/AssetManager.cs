using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Utilities
{
    [DontDestroyOnLoad]
    public class AssetManager : Singleton<AssetManager>
    {
        [ShowInInspector, ReadOnly] private Dictionary<AssetReference, (AsyncOperationHandle handle, object tag)> loaded;
        [ShowInInspector, ReadOnly] private Dictionary<AssetReference, (AsyncOperationHandle handle, object tag)> loading;
    
        protected static Dictionary<string, Sprite> sprites;
        //protected static Dictionary<string, SpriteHandler> sprites_loading;
    
        /*protected class SpriteHandler
    {
        public Task<Sprite> loading;

        public SpriteHandler(string url)
        {
            loading = LoadAndReturnSprite(url);
            sprites_loading.Add(url, this);
        }

        public async Task<Sprite> LoadAndReturnSprite(string url)
        {
            var image = await RequestManager.Instance.LoadImage(url);
            if (image)
            {
                var sprite = Sprite.Create(image, new Rect(0, 0, image.width, image.height), Vector2.zero);
                sprites_loading.Remove(url);
                sprites.Add(url, sprite);
                return sprite;
            }

            return null;
        }
    }*/

#if UNITY_EDITOR
        [CustomValueDrawer("DrawProgress")]
        public string progress;

        private string DrawProgress(string value, GUIContent label)
        {
            StringBuilder str = new StringBuilder();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Loading: " + loading.Count);
            foreach (KeyValuePair<AssetReference, (AsyncOperationHandle handle, object tag)> hit in loading)
            {
                str.Append("|->");
                for (int i = 0; i < 20; i++)
                {
                    if (i < hit.Value.handle.PercentComplete * 20)
                    {
                        str.Append("*");
                    }
                    else
                    {
                        str.Append("-");
                    }
                }
                str.Append("| ");
                str.Append(hit.Value.handle.PercentComplete);
                str.Append(" - ");
                str.Append(hit.Key.editorAsset.name);
                value = str.ToString();
                EditorGUILayout.LabelField(value);
            }
            EditorGUILayout.EndVertical();
            return value;
        }
#endif

        public void GetPercentComplete(ref float[] values)
        {
            if(values == null || values.Length != loading.Count)
            {
                values = new float[loading.Count];
            }

            int i = 0;
            foreach (KeyValuePair<AssetReference, (AsyncOperationHandle handle, object tag)> hit in loading)
            {
                values[i++] = hit.Value.handle.PercentComplete;
            }
        }


        protected override void Setup()
        {
            loaded = new Dictionary<AssetReference, (AsyncOperationHandle handle, object tag)>();
            loading = new Dictionary<AssetReference, (AsyncOperationHandle handle, object tag)>();
        }

        /// <summary>
        /// Load adressable asset or get alredy loaded
        /// </summary>
        /// <param name="asset">asset to load</param>
        /// <param name="tag">tag of asset to group them</param>
        /// <returns>callback when asset was loaded</returns>
        public LoadHandle<T> LoadAsset<T>(AssetReference asset, object tag)
        {
        
            LoadHandle<T> handle = new LoadHandle<T>();

            if (loading.ContainsKey(asset)) //subscribe on callback if the asset is loading
            {
                loading[asset].handle.Completed += v =>
                {
                    handle.Invoke((T)v.Result);
                };
                return handle;
            }

            Instance.StartCoroutine(LoadAsset<T>(asset, handle, tag)); //begin asset loading
            return handle;
        }

        public void Unload(object tag)
        {
            List<AssetReference> toRemove = new List<AssetReference>();
            foreach (KeyValuePair<AssetReference, (AsyncOperationHandle handle, object tag)> hit in loaded)
            {
                if (hit.Value.tag == tag)
                {
                    Addressables.Release(hit.Value.handle);
                    toRemove.Add(hit.Key);
                }
            }
            foreach (AssetReference hit in toRemove)
            {
                loaded.Remove(hit);
            }
        }

        public void Unload(object tag, System.Predicate<AssetReference> predication)
        {
            List<AssetReference> toRemove = new List<AssetReference>();
            foreach (KeyValuePair<AssetReference, (AsyncOperationHandle handle, object tag)> hit in loaded)
            {
                if (hit.Value.tag == tag && predication.Invoke(hit.Key))
                {
                    Addressables.Release(hit.Value.handle);
                    toRemove.Add(hit.Key);
                }
            }
            foreach (AssetReference hit in toRemove)
            {
                loaded.Remove(hit);
            }
        }

        public void UnloadObject(object thing)
        {
            List<AssetReference> toRemove = new List<AssetReference>();
            foreach (KeyValuePair<AssetReference, (AsyncOperationHandle handle, object tag)> hit in loaded)
            {
                if (hit.Value.handle.Result == thing)
                {
                    Addressables.Release(hit.Value.handle);
                    toRemove.Add(hit.Key);
                }
            }
            foreach (AssetReference hit in toRemove)
            {
                loaded.Remove(hit);
            }
        }

        public class LoadHandle<T>
        {
            public System.Action<T> onComplete;

            public void Invoke(T value)
            {
                onComplete?.Invoke(value);
            }
        }

        IEnumerator LoadAsset<T>(AssetReference asset, LoadHandle<T> handle, object tag)
        {
            if (loaded.ContainsKey(asset)) //wait one frame to subscribe on action and return result if the asset is already loaded
            {
                yield return null;
                handle.Invoke((T)loaded[asset].handle.Result);
                yield break;
            }

            AsyncOperationHandle<T> _loading = asset.LoadAssetAsync<T>();

            loading.Add(asset, (_loading, tag));

            yield return _loading;

            loading.Remove(asset);

            loaded.Add(asset, (_loading, tag));

            handle?.Invoke((T)_loading.Result);
        }
        
        public async Task<T> LoadAssetTask<T>(AssetReference asset, object tag)
        {
            if (loaded.ContainsKey(asset)) //return result if the asset is already loaded
            {
                return (T)loaded[asset].handle.Result;
            }

            AsyncOperationHandle _loading;

            if (loading.ContainsKey(asset)) //wait and return result if asset is loading
            {
                _loading = loading[asset].handle;
                await _loading.Task;
            }
            else //load asset and return result
            {
                _loading = asset.LoadAssetAsync<T>();
                loading.Add(asset, (_loading, tag));
                await _loading.Task;
                loading.Remove(asset);
                loaded.Add(asset, (_loading, tag));
            }

            return (T)_loading.Result;
        }
    
        /*public async Task<Sprite> LoadIcon(string url)
    {
        
        Debug.Log($"Load icon from url: {url}");
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        if (sprites == null) sprites = new Dictionary<string, Sprite>();

        if (sprites.ContainsKey(url))
        {
            return sprites[url];
        }
        else
        {
            if (sprites_loading == null) sprites_loading = new Dictionary<string, SpriteHandler>();

            if (sprites_loading.ContainsKey(url))
            {
                return await sprites_loading[url].loading;
            }
            else
            {
                var handler = new SpriteHandler(url);

                var result = await handler.loading;
                if(result)
                {
                    Debug.Log($"Load icon success! Url: {url}");
                }
                else
                {
                    return null;
				}
                return result;
            }
        }
    }*/
    }
}

/*#if UNITY_EDITOR
[CustomEditor(typeof(AssetManager))]
public class AssetManager_Editor : Editor
{
    private AssetManager manager;

    private void OnEnable()
    {
        manager = (AssetManager)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var l = manager.GetLoading();
        EditorGUILayout.LabelField("Loading: " + l.Count);

        foreach (var hit in l)
        {
            StringBuilder str = new StringBuilder();
            str.Append("|->");
            for(int i = 0; i < 20; i++)
            {
                if((float)i / 20 < hit.Value.handle.PercentComplete)
                {
                    str.Append(".");
                }
                else
                {
                    str.Append(" ");
                }
            }
            str.Append("| - ");
            str.Append(hit.Key.editorAsset.name);
            EditorGUILayout.LabelField(str.ToString());
        }
    }
}

#endif*/