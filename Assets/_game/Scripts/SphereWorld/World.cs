using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Character;
using Runtime.Character;
using Sirenix.OdinInspector;
using SphereWorld.Environment.Wind;
using UnityEngine;
using Zenject;

namespace SphereWorld
{
    public class World : MonoInstaller, ILoadAtStart
    {
        [SerializeField] private WorldProfile worldProfile;
        private List<WorldEntity> _entities = new();
        private List<Anchor> _anchors = new();
        private Space _space;
        private TaskCompletionSource<bool> _loadCompletionSource;
        [Inject] private WindSimulation _windSimulation;
        [ShowInInspector, InlineProperty] private Polar? _mainAnchorCoordinates
        {
            get => _space?.Anchor?.Polar ?? null;
        }
        bool ILoadAtStart.enabled
        {
            get => enabled && gameObject.activeInHierarchy;
        }
        
        public override void InstallBindings()
        {
            Container.Bind<WorldProfile>().FromInstance(worldProfile);
        }
        
        public Task Load()
        {
            _space = new Space(worldProfile.rigidPlanetRadiusKilometers);
            Container.Inject(_space);
            _loadCompletionSource = new TaskCompletionSource<bool>();
            SpawnPerson.OnPlayerWasLoaded.Subscribe(ContinueLoad);
            _windSimulation.OnSimulationTickComplete += OnSimulationTickComplete;
            return _loadCompletionSource.Task;
        }

        private void ContinueLoad()
        {
            var playerEntity = SpawnPerson.Instance.Player.GetComponent<WorldEntity>();
            _space.InjectAnchor(DropAnchor(playerEntity.GetPolar()));
            foreach (var entity in GetComponentsInChildren<WorldEntity>())
            {
                RegisterEntity(entity);
            }
            RegisterEntity(playerEntity);

            _loadCompletionSource.SetResult(true);
        }

        private Anchor DropAnchor(Polar polar)
        {
            Anchor anchor = new Anchor();
            anchor.Polar = polar;
            float uniSphereHeight = 1f + polar.height / _space.ZeroHeight;
            Vector3 globalPosition = polar.ToGlobalWithHeight(uniSphereHeight);
            anchor.ParticlePresentationIndex = _windSimulation.AddAnchor(globalPosition, Random.insideUnitSphere * 4);
            _anchors.Add(anchor);
            CollectEntitiesNearToAnchor(anchor);
            return anchor;
        }

        private void CollectEntitiesNearToAnchor(Anchor anchor)
        {
            
        }
        
        public void RegisterEntity(WorldEntity entity)
        {
            _entities.Add(entity);
            entity.InjectSpace(_space);
        }

        private void OnSimulationTickComplete()
        {
            for (var i = 0; i < _anchors.Count; i++)
            {
                Particle p = _windSimulation.GetAnchor(_anchors[i].ParticlePresentationIndex);
                _anchors[i].Polar = Polar.FromUniSphere(p.GetPosition(), _space.ZeroHeight);
            }
        }
    }
}