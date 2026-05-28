using Core.Weapon;
using UnityEngine;

namespace Core.Structure.Damage
{
    public interface IInitAsDamageModel
    {
        public void InitAsDamageModel(ProjectileHandler projectileHandler);
        public void InitAsStructurePart(ProjectileHandler projectileHandler);
    }

    public static class InitAsDamageModelExtensions
    {
        public static void InitAsDamageModel(this Transform root, ProjectileHandler projectileHandler)
        {
            foreach (var damageModelPart in root.GetComponentsInChildren<IInitAsDamageModel>(true))
            {
                damageModelPart.InitAsDamageModel(projectileHandler);
            }
        }
        
        public static void InitAsStructurePart(this Transform root, ProjectileHandler projectileHandler)
        {
            foreach (var damageModelPart in root.GetComponentsInChildren<IInitAsDamageModel>(true))
            {
                damageModelPart.InitAsStructurePart(projectileHandler);
            }
        }
    }
}