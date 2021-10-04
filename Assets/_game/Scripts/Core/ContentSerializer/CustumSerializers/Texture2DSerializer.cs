using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ContentSerializer;
using UnityEngine;

namespace ContentSerializer
{
    public class Texture2DSerializer : ICustomSerializer
    {

        public string Serialize(object source, ISerializationContext context, int idx)
        {
            Texture2D tex = (Texture2D) source;

            switch (idx)
            {
                case 1:
                    return $"Tex_{tex.name}_{tex.GetInstanceID()}";
                case 2:
                    //Debug.Log(tex.graphicsFormat);
                    return (tex.graphicsFormat).ToString();
#if UNITY_EDITOR
                default:

                    var filePath = UnityEditor.AssetDatabase.GetAssetPath(tex);

                    var name = $"Tex_{tex.name}_{tex.GetInstanceID()}";
                    try
                    {
                        File.Copy(filePath, Application.dataPath + PathStorage.BASE_PATH_TEXTURES + "/" + $"{name}.png");
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                    
                    return tex.GetInstanceID().ToString();
#else
                default:
                return "";
#endif
            }
        }

        
        public int GetStringsCount() => 3;

        public void Deserialize(string prefix, ref object source, Dictionary<string, string> hash, ISerializationContext context)
        {
            
        }
    }
}
