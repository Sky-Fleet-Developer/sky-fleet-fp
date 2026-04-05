using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper;
using UnityEngine;

namespace Core.Weapon
{
    [Serializable]
    public class ShellType
    {
        [Serializable]
        private class VfxPerCaliberDiameter
        {
            public InterfaceReference<IVfx, MonoBehaviour> initialVfx;
            public InterfaceReference<IVfx, MonoBehaviour> lifetimeVfx;
            public InterfaceReference<IVfx, MonoBehaviour> defaultHitVfx;
            public int maxCaliberDiameter;
        }
        
        
        public string id;
        [Tooltip("Place items in order of caliber diameter from smallest to largest")]
        [SerializeField] private VfxPerCaliberDiameter[] vfxPerCaliberDiameter;
        
        
        public IVfx GetInitialVfx(int caliber)
        {
            return GetVfxPerCaliberDiameter(caliber).initialVfx.Value;
        }

        public IVfx GetLifetimeVfx(int caliber)
        {
            return GetVfxPerCaliberDiameter(caliber).lifetimeVfx.Value;
        }

        public IVfx GetDefaultHitVfx(int caliber)
        {
            return GetVfxPerCaliberDiameter(caliber).defaultHitVfx.Value;
        }

        private VfxPerCaliberDiameter GetVfxPerCaliberDiameter(int caliber)
        {
            for (var i = 0; i < vfxPerCaliberDiameter.Length; i++)
            {
                if (caliber < vfxPerCaliberDiameter[i].maxCaliberDiameter)
                {
                    return vfxPerCaliberDiameter[i];
                }
            }
            return vfxPerCaliberDiameter[^1];
        }
    }
    
    [CreateAssetMenu(menuName = "SF/ShellTypesSettings")]
    public class ShellTypesSettings : ScriptableObject
    {
        [SerializeField] private ShellType[] shellTypeSettings;
        private Dictionary<string, ShellType> _dataOverType;
        
        public ShellType GetShellTypeSettings(string id)
        {
            _dataOverType ??= shellTypeSettings.ToDictionary(x => x.id, x => x);
            return _dataOverType[id];
        }
    }
}