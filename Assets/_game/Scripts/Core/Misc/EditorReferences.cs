using System.Linq;
using Core.Ai;
using Core.Character.Stuff;
using Core.Configurations;
using Core.Data;
using Core.Weapon;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Core.Misc
{
#if UNITY_EDITOR
    public static class EditorReferences
    {
        public static TablePrefabs TablePrefabsEditor;
        public static ItemsTable ItemsTableEditor;
        public static TableRelations RelationsTableEditor;
        public static StuffSlotsTable StuffSlotsTableEditor;
        
        static EditorReferences()
        {
            TablePrefabsEditor = AssetDatabase.LoadAssetAtPath<TablePrefabs>("Assets/_game/Data/Resources/TablePrefabs.asset");
            var gameData = AssetDatabase.LoadAssetAtPath<GameData>("Assets/_game/Data/Resources/GameData.asset");
            ItemsTableEditor = gameData.GetChildAssets<ItemsTable>().First();
            RelationsTableEditor = gameData.GetChildAssets<TableRelations>().First();
            StuffSlotsTableEditor = gameData.GetChildAssets<StuffSlotsTable>().First();
        }
    }
#endif
}