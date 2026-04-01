using System.Linq;
using Core.Ai;
using Core.Character.Stuff;
using Core.Configurations;
using Core.Data;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Core.Misc
{
#if UNITY_EDITOR
    public static class EditorReferences
    {
        public static TableRelations RelationsTableEditor;
        public static ItemsTable ItemsTableEditor;
        public static TablePrefabs TablePrefabsEditor;
        public static StuffSlotsTable StuffSlotsTableEditor;

        static EditorReferences()
        {
            var gameData = AssetDatabase.LoadAssetAtPath<GameData>("Assets/_game/Data/Resources/GameData.asset");
            ItemsTableEditor = gameData.GetChildAssets<ItemsTable>().First();
            RelationsTableEditor = gameData.GetChildAssets<TableRelations>().First();
            TablePrefabsEditor = AssetDatabase.LoadAssetAtPath<TablePrefabs>("Assets/_game/Data/Resources/TablePrefabs.asset");
            StuffSlotsTableEditor = gameData.GetChildAssets<StuffSlotsTable>().First();
        }
    }
#endif
}