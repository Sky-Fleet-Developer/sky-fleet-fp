using Core.Structure.Damage;
using Core.Structure.Rigging;
using UnityEngine;

namespace Core.Weapon
{
    [RequireComponent(typeof(Collider))]
    public class Armor : MonoBehaviour, IInitAsDamageModel
    {
        public ArmorData armorData = new ArmorData { durability = 2200, thickness = 20 };

        public void InitAsDamageModel(ProjectileHandler projectileHandler)
        {
            gameObject.layer = LayerMask.NameToLayer("Damagable");
            projectileHandler.RegisterArmor(GetComponent<Collider>().GetInstanceID(), armorData);
        }

        public void InitAsStructurePart(ProjectileHandler projectileHandler)
        {
            enabled = false;
        }
    }
}