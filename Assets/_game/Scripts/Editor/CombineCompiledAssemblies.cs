using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Windows.Forms;
using Application = UnityEngine.Application;

#if UNITY_EDITOR
namespace UnityEditor
{
    [CreateAssetMenu(menuName = "CombineCompiledAssemblies")]
    public class CombineCompiledAssemblies : ScriptableObject
    {
        public SerializedFilePath[] combinedFiles;
        [FolderPath(AbsolutePath = true)] public string path = string.Empty;

        [MenuItem("Tools/ExportLibrariesForMod")]
        public static void CollectStatic()
        {
            CombineCompiledAssemblies instance =
                AssetDatabase.LoadAssetAtPath<CombineCompiledAssemblies>(
                    "Assets/_game/Scripts/Editor/CombineCompiledAssemblies.asset");
            instance.Collect();
        }
        
        [Button]
        public void Collect()
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
            string filename = string.Empty;
            foreach (SerializedFilePath serializedFilePath in combinedFiles)
            {
                filename = serializedFilePath.filePath.Split(new[] {'/', '\\'}).Last();
                File.Copy(serializedFilePath.filePath, $"{path}/{filename}");
            }

            string p = $"{path}/";
            p = p.Replace('/', '\\');
            Process.Start("Explorer.exe", @"/select," + p);
        }
    }

    [System.Serializable, InlineProperty]
    public class SerializedFilePath
    {
        [HorizontalGroup("path")] public string filePath = string.Empty;

        [Button, HorizontalGroup("path")]
        public async void SelectTarget()
        {
            await Task.Yield();
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Application.dataPath;
                openFileDialog.Filter = "assembly files (*.dll)|*.dll";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                }
            }

        }
    }

}
#endif
