using System;
using System.Collections.Generic;
using Core.Configurations.GoogleSheets;
using Sirenix.OdinInspector;
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
    [CreateAssetMenu(menuName = "Configs/LocalizationTableImporter")]
    public class LocalizationTableImporter : Table<LocalizationItem>
    {
        [SerializeField] private LocalizationItem[] data;
        [SerializeField] private StringTableCollection tableCollection;
        public override string TableName => "Localization";

        public override LocalizationItem[] Data
        {
            get => data;
            protected set
            {
                data = value;
                Test(value);
            } 
        }

        [Button]
        public void Test(LocalizationItem[] data)
        {
            foreach (var tableCollectionTable in tableCollection.Tables)
            {
                if (tableCollectionTable.asset is StringTable stringTable)
                {
                    stringTable.Clear();
                    foreach (var localizationItem in data)
                    {
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
            }
        }
    }
}
