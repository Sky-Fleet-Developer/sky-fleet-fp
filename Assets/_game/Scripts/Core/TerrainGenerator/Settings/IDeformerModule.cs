using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    /// <summary>
    /// realtime rules and data to apply deformer in current rect to current channel
    /// </summary>
    public interface IDeformerModule
    {
        IDeformer Core { get; set; }

        void Init(IDeformer core);

        void ReadFromTerrain();

        void WriteToChannel(DeformationChannel channel);
        //TODO: void WriteToChannelIfMatch(DeformationChannel channel, Rect rect);
    }
}