using Core.Grass;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Runtime.Grass
{
    /// <summary>
    /// A sphere around this transform that pushes the grass out of it: characters, vehicles, blasts.
    /// Needs injection (an object spawned through the container, or a scene root) to find the grass.
    /// </summary>
    public class GrassPusher : MonoBehaviour, IGrassPusher
    {
        [SerializeField] private Vector3 offset;
        [SerializeField, MinValue(0.01f)] private float radius = 0.6f;
        [PropertyTooltip("Lean of the grass at the center, in plant heights: about 1 lays it down")]
        [SerializeField, PropertyRange(0, 2)] private float strength = 1;

        private IGrass _grass;

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0.01f, value);
        }

        public float Strength
        {
            get => strength;
            set => strength = value;
        }

        public GrassPusherData Pusher => new ()
        {
            Center = transform.TransformPoint(offset),
            Radius = radius,
            Strength = strength,
        };

        [Inject]
        private void Construct([InjectOptional] IGrass grass)
        {
            _grass = grass;
            if (isActiveAndEnabled)
            {
                _grass?.AddPusher(this);
            }
        }

        private void OnEnable() => _grass?.AddPusher(this);

        private void OnDisable() => _grass?.RemovePusher(this);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(transform.TransformPoint(offset), radius);
        }
    }
}
