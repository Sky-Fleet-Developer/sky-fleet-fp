using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.ModContent
{
    public class ItemModPropertyUI : ItemPointer
    {
        public enum PropertyType{
            Header = 0,
            Item = 1,
        }

        public ItemModPropertyUI()
        {
            properties.Add("TypeProperty", SetPropertyType);
        }

        private void SetPropertyType(object value)
        {
            Text text = GetPointer<Text>("Text");
            PropertyType type = (PropertyType)value;
            if(type == PropertyType.Header)
            {
                text.alignment = TextAnchor.MiddleLeft;
            }
            else if(type == PropertyType.Item)
            {
                text.alignment = TextAnchor.MiddleRight;
            }
        }
    }

}