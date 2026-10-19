using UnityEngine;

namespace Core.World
{
    /// <summary>
    /// Put next to an entity installer to give the entity an <see cref="EntityIdentity"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class EntityIdentityAuthoring : MonoBehaviour
    {
        [SerializeField, Tooltip("Readable name for the narrative system ([a-z0-9_.]). Must be unique in the world.")]
        private string alias;

        public string Alias => EntityAliases.Normalize(alias);

        public static void ApplyTo(GameObject source, IWorldEntity entity)
        {
            if (source.TryGetComponent(out EntityIdentityAuthoring authoring) && authoring.Alias != null)
            {
                entity.Components.Set(new EntityIdentity(authoring.Alias));
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            alias = EntityAliases.Normalize(alias);
            if (alias == null || !gameObject.scene.IsValid()) return;
            foreach (var other in FindObjectsByType<EntityIdentityAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != this && other.alias == alias)
                {
                    Debug.LogWarning($"Entity alias '{alias}' is used by '{name}' and '{other.name}'", this);
                }
            }
        }
#endif
    }
}
