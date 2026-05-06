using System;
using UnityEngine;

namespace Core.Weapon
{
    public class Armor : MonoBehaviour
    {
        public float thickness;
        public float durability = 2200;

        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Damagable");
        }
    }
}