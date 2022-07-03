using UnityEngine;

namespace Core.Explorer.Content
{
    public abstract class ModExe
    {
        protected Mod Mod;

        public ModExe(Mod mod)
        {
            Mod = mod;
        }
        
        public abstract void Main();
    }
}
