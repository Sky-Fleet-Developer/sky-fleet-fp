using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Trees
{
    /// <summary>
    /// A tree baked into pictures from the directions of a hemisphere (<see cref="ImpostorLayout"/>), for the far ring
    /// where a tree is drawn as one quad. Made by the Editor baker, not by hand.
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Tree Impostor")]
    public class TreeImpostor : ScriptableObject
    {
        [Title("Pictures")]
        [PropertyTooltip("Color in rgb, how much of the pixel the tree covers in a. All the frames in a grid")]
        [SerializeField] private Texture2D albedo;
        [PropertyTooltip("Normal in object space in rgb (0.5 + 0.5 * n), depth in a: 0 at the side of the frame facing " +
                         "the viewer, 1 at the far side")]
        [SerializeField] private Texture2D normalDepth;
        [PropertyTooltip("Frames along a side of the grid")]
        [SerializeField, MinValue(1)] private int framesPerSide = 8;

        [Title("Frame")]
        [PropertyTooltip("Center of every frame, in the space of the tree prefab")]
        [SerializeField] private Vector3 center;
        [PropertyTooltip("Half the size of a frame in meters: the frames are squares around the center, orthographic")]
        [SerializeField, MinValue(0.01f)] private float radius = 1;

        public Texture2D Albedo => albedo;
        public Texture2D NormalDepth => normalDepth;
        public int FramesPerSide => framesPerSide;
        public Vector3 Center => center;
        public float Radius => radius;

        public void Set(Texture2D albedo, Texture2D normalDepth, int framesPerSide, Vector3 center, float radius)
        {
            this.albedo = albedo;
            this.normalDepth = normalDepth;
            this.framesPerSide = framesPerSide;
            this.center = center;
            this.radius = radius;
        }
    }
}
