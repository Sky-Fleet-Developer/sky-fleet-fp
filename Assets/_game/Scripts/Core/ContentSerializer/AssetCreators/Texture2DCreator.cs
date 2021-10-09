using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.ContentSerializer.AssetCreators
{
    public class Texture2DCreator : IAssetCreator
    {
        [ItemCanBeNull]
        public async Task<Object> CreateInstance(string prefix, Dictionary<string, string> cache,
            ISerializationContext context)
        {
            string name = cache[prefix + "_1"];
            if (context.IsCurrentlyBuilded)
            {
                Debug.Log($"Search texture in path: {Application.dataPath}{PathStorage.BASE_PATH_TEXTURES}/{name}.png");
                return await CreateInstance($"{Application.dataPath}{PathStorage.BASE_PATH_TEXTURES}/{name}.png");
            }
            else
            {
                Debug.Log($"Search texture in path: {context.ModFolderPath}{PathStorage.MOD_RELETIVE_PATH_TEXTURES}/{name}.png");
                return await CreateInstance($"{context.ModFolderPath}{PathStorage.MOD_RELETIVE_PATH_TEXTURES}/{name}.png");
            }
        }


        public static async Task<Texture2D> CreateInstance(string path)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(path))
            {
                await request.SendWebRequest();
                if (request.error != null)
                {
                    Debug.Log(request.error);
                }
                else
                {
                    var tex = DownloadHandlerTexture.GetContent(request);
                    if(path.Contains("Normal"))
                    {
                        var tex2 = new Texture2D(tex.width, tex.height, tex.format, true, true);
                        tex2.SetPixels(tex.GetPixels());
                        tex2.Apply();
                        return tex2;
                    }
                    else
                    {
                        tex.Apply();
                        return tex;
                    }
                    
                }
            }
            return null;
        }
    }
}
