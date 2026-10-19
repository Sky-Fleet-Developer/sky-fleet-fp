using System.IO;
using Core.World;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public static class ClearWorldSave
    {
        [MenuItem("SF/World/Clear world save")]
        private static void Clear()
        {
            string path = Path.GetDirectoryName(new DefaultWorldSaveSlot().GetLocationDirectory("x"));
            if (!Directory.Exists(path))
            {
                Debug.Log($"World save is empty ({path})");
                return;
            }

            if (!EditorUtility.DisplayDialog("Clear world save", $"Delete everything in\n{path}?", "Delete", "Cancel")) return;
            Directory.Delete(path, true);
            Debug.Log($"World save deleted ({path})");
        }
    }
}
