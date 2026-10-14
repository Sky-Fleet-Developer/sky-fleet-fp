using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.TerrainGenerator.Surfaces
{
    public enum SurfaceChannelMode
    {
        /// <summary>Channel i uses surface i of the terrain settings.</summary>
        Default = 0,
        /// <summary>The channel uses the given surface type.</summary>
        Surface = 1,
        /// <summary>The channel paints nothing.</summary>
        Ignore = 2,
    }

    /// <summary>Surface type painted by one channel of a source alphamap.</summary>
    [Serializable]
    public struct SurfaceChannel
    {
        [SerializeField, HorizontalGroup, HideLabel] private SurfaceChannelMode mode;
        [SerializeField, HorizontalGroup, HideLabel, ShowIf(nameof(mode), SurfaceChannelMode.Surface)]
        private SurfaceType surface;

        public SurfaceChannelMode Mode => mode;
        /// <summary>Used when <see cref="Mode"/> is <see cref="SurfaceChannelMode.Surface"/>.</summary>
        public SurfaceType Surface => surface;

        public SurfaceChannel(SurfaceChannelMode mode, SurfaceType surface = null)
        {
            this.mode = mode;
            this.surface = surface;
        }
    }
}
