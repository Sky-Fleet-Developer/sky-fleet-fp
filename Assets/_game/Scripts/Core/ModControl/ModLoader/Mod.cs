using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ContentSerializer;

namespace Core.Mods
{
    public class Mod : MonoBehaviour
    {
        private SerializationModule modul;

        public Mod(SerializationModule modul, LinkedList<Object> materialAsset)
        {
            this.modul = modul;
        }
    }
}