using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    public interface IDeformerLayerSetting
    {
        Deformer Core { get; set; }

        void Init(Deformer core);

        void ReadFromTerrain();

        void WriteToTerrain(Terrain[] terrains);
    }
}