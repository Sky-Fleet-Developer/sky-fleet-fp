#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Core.Configurations.GoogleSheets;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Localization.Import
{
    [Serializable]
    public class LocalizationItem
    {
        public string key;
        public string ru;
        public string en;
    }
    [CreateAssetMenu(menuName = "SF/Configs/LocalizationTableImporter")]
    public class LocalizationTableImporter : Table<LocalizationItem>
    {
        [SerializeField] private LocalizationItem[] data;
        [SerializeField] private StringTableCollection tableCollection;
        [SerializeField] private SharedTableData sharedTableData;
        public override string TableName => "Localization";

        protected override LocalizationItem[] Data
        {
            set
            {
                data = value;
                ApplyToLocalizationAssets(value);
            } 
        }

        [Button]
        public void ApplyToLocalizationAssets(LocalizationItem[] data)
        {
            sharedTableData.Clear();
            foreach (var tableCollectionTable in tableCollection.Tables)
            {
                if (tableCollectionTable.asset is StringTable stringTable)
                {
                    stringTable.Clear();
                    foreach (var localizationItem in data)
                    {
                        sharedTableData.AddKey(localizationItem.key);
                        switch (stringTable.LocaleIdentifier.Code)
                        {
                            case "ru-RU":
                                stringTable.AddEntry(localizationItem.key, localizationItem.ru);
                                break;
                            case "en-US":
                                stringTable.AddEntry(localizationItem.key, localizationItem.en);
                                break;
                        }
                    }
                }
                #if UNITY_EDITOR
                EditorUtility.SetDirty(tableCollectionTable.asset);
                #endif
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(tableCollection);
            EditorUtility.SetDirty(sharedTableData);
#endif
        }
    }
}
#endif
