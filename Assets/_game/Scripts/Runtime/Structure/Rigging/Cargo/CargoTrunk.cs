using System;
using System.Collections.Generic;
using Cinemachine;
using Core.Configurations;
using Core.Game;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Cargo;
using Runtime.Cargo;
using Runtime.Cargo.Input;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [ShowInInspector] public override float Mass => base.Mass + _cargoMass;
        [ShowInInspector] public override Vector3 LocalCenterOfMass => _localCenterOfMass;

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
                var max = volumeAnchor.max;
                for (pointer.x = volumeAnchor.min.x; pointer.x <= max.x; pointer.x++)
                {
                    for (pointer.y = volumeAnchor.min.y; pointer.y <= max.y; pointer.y++)
                    {
                        for (pointer.z = volumeAnchor.min.z; pointer.z <= max.z; pointer.z++)
                        {
                            if (!IsCellExcluded(pointer))
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
            _cargoToPlace = cargo;
            _volumeToPlace = volume;
            
            foreach (var point in volume)
            {
                var p = point + position;
                if (!_space.Contains(p) || _content.ContainsKey(p))
                {
                    return false;
                }
            }

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
            var cargoRigidbody = _cargoToPlace.transform.GetComponent<Rigidbody>();
            cargoRigidbody.isKinematic = true;
            _cargo.Add(_cargoToPlace);
            RefreshMass();
        }

        private void RefreshMass()
        {
            _localCenterOfMass = transform.position * base.Mass;
            _cargoMass = base.Mass;
            foreach (var cargo in _cargo)
            {
                if (_cargoToPlace is IMass mass)
                {
                    _cargoMass += mass.Mass;
                    _localCenterOfMass += _cargoToPlace.transform.TransformPoint(mass.LocalCenterOfMass) * mass.Mass;
                }
                else
                {
                    var rb = cargo.transform.GetComponent<DynamicWorldObject>();
                    var massParams = rb.GetMass();
                    _cargoMass += massParams.w;
                    _localCenterOfMass += _cargoToPlace.transform.TransformPoint(massParams) * massParams.w;
                    rb.ConvertToStatic();
                }
            }
            
            _localCenterOfMass = transform.InverseTransformPoint(_localCenterOfMass / _cargoMass);
            (Structure as IDynamicStructure)?.RecalculateMass();
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
            var volume = _prefabVolumes.GetProfile(cargo).GetVolume();
            int bottom = volume[0].y;
            foreach (var v in volume)
            {
                bottom = Mathf.Min(bottom, v.y);
            }
            _cargoViewPosition = new Vector3Int(0, -bottom, 0);
            cargoVolumeView.SetVolume(volume, _cargoViewPosition, _prefabVolumes.ParticleSize);
            _cargoToPlace = cargo;
        }

        void ICargoTrunkPlayerInterface.Move(Vector3Int delta)
        {
            _cargoViewPosition += delta;
            cargoVolumeView.Move(delta);
            Debug.Log(_cargoViewPosition);
            cargoVolumeView.SetCollisionMask(GetCollisionMask());
        }

        bool ICargoTrunkPlayerInterface.Confirm()
        {
            if (TryPlaceCargo(_cargoToPlace, _cargoViewPosition, out var handler))
            {
                handler.PlaceAction?.Invoke();
                return true;
            }
            return false;
        }

        private IEnumerable<(Vector3Int, int)> GetCollisionMask()
        {
            IReadOnlyList<Vector3Int> volume = _prefabVolumes.GetProfile(_cargoToPlace).GetVolume();

            foreach (var point in volume)
            {
                var p = point + _cargoViewPosition;
                if (!_space.Contains(p))
                {
                    yield return (point, 1);
                }
                else if(_content.ContainsKey(p))
                {
                    yield return (point, 2);
                }
                else
                {
                    yield return (point, 0);
                }
            }
        }

        void ICargoTrunkPlayerInterface.ExitPlacement()
        {
            placementCamera.gameObject.SetActive(false);
            trunkVolumeView.gameObject.SetActive(false);
            cargoVolumeView.Clear();
        }
    }
}