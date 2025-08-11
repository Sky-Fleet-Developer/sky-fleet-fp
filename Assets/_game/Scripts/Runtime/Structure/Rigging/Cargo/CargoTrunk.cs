using System;
using System.Collections.Generic;
using Cinemachine;
using Core.Configurations;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Cargo;
using Runtime.Cargo;
using UnityEngine;
using Zenject;

namespace Runtime.Structure.Rigging.Cargo
{
    public class CargoTrunk : Block, ICargoTrunkPlayerInterface
    {
        [SerializeField] private BoundsInt[] volumeAnchors;
        [SerializeField] private BoundsInt[] excludeVolumeAnchors;
        [SerializeField] private CinemachineVirtualCamera placementCamera;
        [SerializeField] private TrunkVolumeView trunkVolumeView;
        [SerializeField] private CargoVolumeView cargoVolumeView;
        [Inject] private PrefabVolumeProcessor _prefabVolumes;
        private Dictionary<Vector3Int, ITablePrefab> _content = new();
        private HashSet<Vector3Int> _space = new();
        private PlaceCargoHandler _rejectHandler = PlaceCargoHandler.Empty;
        private PlaceCargoHandler _acceptHandler;
        private uint _placeCounter = 1;
        private uint _placeToken = 0;
        private ITablePrefab _cargoToPlace;
        private IReadOnlyList<Vector3Int> _volumeToPlace;
        private Vector3Int _positionToPlace;
        private float _cargoMass;
        private Vector3 _localCenterOfMass;
        private readonly List<ITablePrefab> _cargo = new ();
        public override float Mass => base.Mass + _cargoMass;
        public override Vector3 LocalCenterOfMass => _localCenterOfMass;

        private void Awake()
        {
            placementCamera.gameObject.SetActive(false);
            trunkVolumeView.gameObject.SetActive(false);
            _acceptHandler = new PlaceCargoHandler { PlaceAction = PlaceLast };
        }

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            _space.Clear();
            foreach (var volumeAnchor in volumeAnchors)
            {
                var pointer = volumeAnchor.min;
                var max = volumeAnchor.min;
                for (; pointer.x <= max.x; pointer.x++)
                {
                    for (; pointer.y <= max.y; pointer.y++)
                    {
                        for (; pointer.z <= max.z; pointer.z++)
                        {
                            if (!_space.Contains(pointer) && !IsCellExcluded(pointer))
                            {
                                _space.Add(pointer);
                            }
                        }
                    }
                }
            }
        }

        //1. коллайдеры не должны касаться другого груза
        //2. коллайдеры должны быть полностью внутри хотя бы одного объема
        public bool TryPlaceCargo(ITablePrefab cargo, Vector3Int position, out PlaceCargoHandler handler)
        {
            _placeCounter++;
            handler = _rejectHandler;
            IReadOnlyList<Vector3Int> volume = _prefabVolumes.GetProfile(cargo).GetVolume();

            foreach (var point in volume)
            {
                var p = point + position;
                if (!_space.Contains(p) || _content.ContainsKey(p))
                {
                    return false;
                }
            }

            _cargoToPlace = cargo;
            _volumeToPlace = volume;
            _placeToken = _placeCounter;
            _positionToPlace = position;
            handler = _acceptHandler;
            return true;
        }

        private void PlaceLast()
        {
            if (_placeToken != _placeCounter)
            {
                Debug.LogError("CargoTrunk: PlaceToken was expired");
                return;
            }
            _placeToken = 0;
            foreach (var point in _volumeToPlace)
            {
                var p = point + _positionToPlace;
                _content[p] = _cargoToPlace;
            }
            _cargoToPlace.transform.SetParent(transform);
            _cargoToPlace.transform.localPosition = (Vector3)_positionToPlace * _prefabVolumes.ParticleSize;
            var rigidbody = _cargoToPlace.transform.GetComponent<Rigidbody>();
            if (_cargoToPlace is IMass mass)
            {
                Vector3 comInfluence = (_cargoToPlace.transform.localPosition + mass.LocalCenterOfMass) / Mass * mass.Mass;
                _cargoMass += mass.Mass;
                _localCenterOfMass += comInfluence;
            }
            else
            {
                Vector3 comInfluence = (_cargoToPlace.transform.localPosition + rigidbody.centerOfMass) / Mass * rigidbody.mass;
                _cargoMass += rigidbody.mass;
                _localCenterOfMass += comInfluence;
            }
            rigidbody.isKinematic = true;
            _cargo.Add(_cargoToPlace);
        }

        private bool IsCellExcluded(Vector3Int cell)
        {
            foreach (BoundsInt excludeVolumeAnchor in excludeVolumeAnchors)
            {
                if (excludeVolumeAnchor.Contains(cell))
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3Int _cargoViewPosition;
        void ICargoTrunkPlayerInterface.EnterPlacement(ITablePrefab cargo)
        {
            placementCamera.gameObject.SetActive(true);
            trunkVolumeView.gameObject.SetActive(true);
            trunkVolumeView.SetVolume(volumeAnchors[0], _prefabVolumes.ParticleSize);
            cargoVolumeView.SetVolume(_prefabVolumes.GetProfile(cargo).GetVolume(), _prefabVolumes.ParticleSize);
            _cargoViewPosition = Vector3Int.zero;
        }

        void ICargoTrunkPlayerInterface.MoveTo(Vector3Int position)
        {
            cargoVolumeView.Move(position - _cargoViewPosition);
            _cargoViewPosition = position;
        }

        void ICargoTrunkPlayerInterface.ExitPlacement()
        {
            placementCamera.gameObject.SetActive(false);
            trunkVolumeView.gameObject.SetActive(false);
            cargoVolumeView.Clear();
        }
    }
}