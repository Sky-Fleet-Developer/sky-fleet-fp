using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator
{
    [System.Serializable]
    public class DeformerLayerSetting : ScriptableObject
    {
        protected Deformer core;

        public void Init(Deformer core)
        {
            this.core = core;
        }

        public virtual void ReadFromTerrain(Terrain[] terrain)
        {

        }

        public virtual void WriteToTerrain(Terrain[] terrain)
        {

        }


    }
}