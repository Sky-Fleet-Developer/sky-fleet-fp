using System;
using AYellowpaper;
using UnityEngine;

namespace Core.Weapon
{
    [Serializable]
    public class ProjectileConfig
    {
        //public ProjectileData data;
        public InterfaceReference<IVfx, MonoBehaviour> initialVfx;
        public InterfaceReference<IVfx, MonoBehaviour> chargeVfx;

        //public ProjectileConfig(ProjectileData projectileData)
        //{
        //    data = projectileData;
        //}
    }
}