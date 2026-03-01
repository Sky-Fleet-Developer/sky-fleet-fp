using Core.Configurations;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public partial class PrefabEntityInstaller : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string prefabId;
        [Inject] private WorldSpace _worldSpace;

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            Bootstrapper.OnLoadComplete.Subscribe(() =>
            {
                var instance = GetComponent<ITablePrefab>();
                if (instance != null)
                {
                    _worldSpace.AddEntity(new PrefabEntity(instance));
                }
                else
                {
                    _worldSpace.AddEntity(new PrefabEntity(prefabId, transform.position, transform.rotation));
                }
                Destroy(this);
            });
        }
    }
}