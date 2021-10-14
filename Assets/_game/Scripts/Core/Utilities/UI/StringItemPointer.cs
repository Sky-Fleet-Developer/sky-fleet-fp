using UnityEngine;
using UnityEngine.UI;

namespace Core.Utilities.UI
{
    public class StringItemPointer : ItemPointer
    {
        public enum PropertyType{
            Header = 0,
            Item = 1,
        }
        
        public override void SetVisual(params object[] args)
        {
            Text text = GetPointer<Text>("Text");
            PropertyType type = (PropertyType)args[0];
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