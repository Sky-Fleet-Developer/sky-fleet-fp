using Sirenix.OdinInspector;
using System.Collections.Generic;
using Core.Scatter;
using UnityEngine;
using Zenject;

namespace Runtime.Scatter
{
    /// <summary>
    /// Keeps the scattered details (grass, small stones) out of a box, sphere or polygon around this transform,
    /// following it as it moves. Needs injection (an object spawned through the container, or a scene root) to find
    /// the scatter system.
    /// </summary>
    public class ScatterExclusionArea : MonoBehaviour, IScatterArea
    {
        [SerializeField] private ScatterAreaShape shape = ScatterAreaShape.Box;
        [PropertyTooltip("Box: size in meters, centered on the transform")]
        [SerializeField] private Vector3 size = new (4, 4, 4);
        [PropertyTooltip("Sphere: radius in meters")]
        [SerializeField, MinValue(0)] private float radius = 2;
        [PropertyTooltip("Polygon: points in local x, z, in order around the area")]
        [SerializeField] private List<Vector2> polygon = new () { new (-2, -2), new (-2, 2), new (2, 2), new (2, -2) };
        [PropertyTooltip("Polygon: local heights of the bottom and the top of the area")]
        [SerializeField] private Vector2 polygonHeight = new (-2, 4);
        [PropertyTooltip("Meters outside the area over which the details grow back to their full size")]
        [SerializeField, MinValue(0)] private float edge = 0.5f;

        private IScatter _scatter;

        public ScatterAreaData Area => new ()
        {
            Shape = shape,
            // Unscaled, so the sizes stay in meters
            WorldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse,
            Size = shape switch
            {
                ScatterAreaShape.Sphere => new Vector3(radius, 0, 0),
                ScatterAreaShape.Polygon => new Vector3(0, polygonHeight.x, polygonHeight.y),
                _ => size,
            },
            Edge = edge,
            Polygon = polygon,
        };

        [Inject]
        private void Construct([InjectOptional] IScatter scatter)
        {
            _scatter = scatter;
            if (isActiveAndEnabled)
            {
                _scatter?.AddArea(this);
            }
        }

        private void OnEnable() => _scatter?.AddArea(this);

        private void OnDisable() => _scatter?.RemoveArea(this);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            switch (shape)
            {
                case ScatterAreaShape.Box:
                    Gizmos.DrawWireCube(Vector3.zero, size);
                    break;
                case ScatterAreaShape.Sphere:
                    Gizmos.DrawWireSphere(Vector3.zero, radius);
                    break;
                case ScatterAreaShape.Polygon:
                    for (int i = 0; i < polygon.Count; i++)
                    {
                        Vector2 a = polygon[i];
                        Vector2 b = polygon[(i + 1) % polygon.Count];
                        Gizmos.DrawLine(new Vector3(a.x, polygonHeight.x, a.y), new Vector3(b.x, polygonHeight.x, b.y));
                        Gizmos.DrawLine(new Vector3(a.x, polygonHeight.y, a.y), new Vector3(b.x, polygonHeight.y, b.y));
                        Gizmos.DrawLine(new Vector3(a.x, polygonHeight.x, a.y), new Vector3(a.x, polygonHeight.y, a.y));
                    }
                    break;
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
