using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    public interface IDeformerLayerSetting
    {
        Deformer Core { get; set; }

        void Init(Deformer core);

        void ReadFromTerrain(Terrain[] terrain);

        void WriteToTerrain(Terrain[] terrain);
    }
}