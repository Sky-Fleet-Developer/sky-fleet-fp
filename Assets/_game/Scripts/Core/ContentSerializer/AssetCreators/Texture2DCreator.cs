using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContentSerializer;
using Core.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;

namespace ContentSerializer
{
    public class Texture2DCreator : IAssetCreator
    {
        [ItemCanBeNull]
        public async Task<Object> CreateInstance(string prefix, Dictionary<string, string> hash, ISerializationContext context)
        {
            /*var format = (GraphicsFormat) int.Parse(hash[prefix + "_1"]);
            var width = int.Parse(hash[prefix + "_2"]);
            var height = int.Parse(hash[prefix + "_3"]);
            var mipCount = int.Parse(hash[prefix + "_4"]);
            var tex = new Texture2D(width, height, format, mipCount > 0 ? TextureCreationFlags.MipChain : TextureCreationFlags.None);*/
            string name = hash[prefix + "_1"];
            Debug.Log("Search texture in path: " + Application.dataPath + "/Mod/" + name + ".png");
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(Application.dataPath + "/Mod/" + name + ".png"))
            {
                await request.SendWebRequest();
                if (request.error != null)
                {
                    Debug.Log(request.error);
                }
                else
                {
                    GraphicsFormat format = (GraphicsFormat)System.Enum.Parse(typeof(GraphicsFormat), hash[prefix + "_2"]);
                    var tex = DownloadHandlerTexture.GetContent(request);
                    if (format == GraphicsFormat.RGBA_DXT1_SRGB)
                    {
                        tex.Apply();
                        return tex;
                    }
                    
                    var tex2 = new Texture2D(tex.width, tex.height, tex.format, true, format != GraphicsFormat.RGBA_DXT1_SRGB);
                    tex2.SetPixels(tex.GetPixels());
                    tex2.Apply();
                    return tex2;
                }
            }
            return null;
        }
    }
}
