using System;
using System.Collections.Generic;
using Core.Cargo;
using Core.Character;
using Core.Configurations;
using Core.Data;
using Core.Game;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Cargo;
using Core.World;
using Runtime.Cargo;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Runtime.Structure.Rigging.Cargo
{
    public class CargoTrunk : Block, ICargoTrunkPlayerInterface, ICargoUnloadingPlayerHandler, IInteractiveObject
    {
        [SerializeField] private BoundsInt[] volumeAnchors;
        [SerializeField] private BoundsInt[] excludeVolumeAnchors;
        [SerializeField] private CinemachineCamera placementCamera;
        [SerializeField] private CinemachineCamera unloadCamera;
        [SerializeField] private TrunkVolumeView trunkVolumeView;
        [SerializeField] private CargoVolumeView cargoVolumeView;
        [Inject] private PrefabVolumeProcessor _prefabVolumes;
        private Dictionary<Vector3Int, ITablePrefab> _content = new();
        private HashSet<Vector3Int> _space = new();
        private PlaceCargoHandler _rejectHandler = PlaceCargoHandler.Empty;
        private PlaceCargoHandler _loadAcceptHandler;
        private PlaceCargoHandler _unloadAcceptHandler;
        private uint _placeCounter = 1;
        private uint _placeToken = 0;
        private ITablePrefab _cargoToPlace;
        private IReadOnlyList<Vector3Int> _volumeToPlace;
        private Vector3Int _positionToPlace;
        private float _cargoMass;
        private Vector3 _localCenterOfMass;
        private readonly List<ITablePrefab> _cargo = new ();
        private bool _visualizeUnloadCargo;
        private Vector3 _cargoVolumeViewInitialPosition;
        [ShowInInspector] public override float Mass => base.Mass + _cargoMass;
        [ShowInInspector] public override Vector3 LocalCenterOfMass => _localCenterOfMass;
        IEnumerable<ITablePrefab> ICargoUnloadingHandler.AvailableCargo => _cargo;

        private void Awake()
        {
            placementCamera.gameObject.SetActive(false);
            unloadCamera.gameObject.SetActive(false);
            trunkVolumeView.gameObject.SetActive(false);
            _loadAcceptHandler = new PlaceCargoHandler { PlaceAction = PlaceLast };
            _unloadAcceptHandler = new PlaceCargoHandler { PlaceAction = UnloadLast };
            _cargoVolumeViewInitialPosition = cargoVolumeView.transform.localPosition;
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
                if (!_space.Contains(p) || _content.TryGetValue(p, out ITablePrefab content) && content != null && content != cargo)
                {
                    return false;
                }
            }

            _placeToken = _placeCounter;
            _positionToPlace = position;
            handler = _loadAcceptHandler;
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
            bool isNewElement = !_cargo.Contains(_cargoToPlace);
            if (isNewElement)
            {
                _cargo.Add(_cargoToPlace);
            }
            RefreshMass(isNewElement);
        }

        private void RefreshMass(bool isNewElement)
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
                    if (isNewElement)
                    {
                        rb.ConvertToStatic();
                        rb.OnMassChanged += OnCargoMassChanged;
                    }
                }
            }
            
            _localCenterOfMass = transform.InverseTransformPoint(_localCenterOfMass / _cargoMass);
            (Structure as IDynamicStructure)?.RecalculateMass();
        }

        private void OnCargoMassChanged()
        {
            RefreshMass(false);
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
        private ITablePrefab _lastUnloadCandidatae;
        private Vector3 _unloadWorldPoint;
        private Quaternion _unloadRotation;

        void ICargoTrunkPlayerInterface.EnterPlacement(ITablePrefab cargo)
        {
            placementCamera.gameObject.SetActive(true);
            trunkVolumeView.gameObject.SetActive(true);
            cargoVolumeView.transform.localPosition = _cargoVolumeViewInitialPosition;
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
                else if(_content.TryGetValue(p, out ITablePrefab content) && content != null)
                {
                    if (content == _cargoToPlace)
                    {
                        yield return (point, 3);
                    }
                    else
                    {
                        yield return (point, 2);
                    }
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

        void ICargoUnloadingPlayerHandler.Enter()
        {
            unloadCamera.gameObject.SetActive(true);
            _visualizeUnloadCargo = true;
        }

        void ICargoUnloadingPlayerHandler.Exit()
        {
            unloadCamera.gameObject.SetActive(false);
            _visualizeUnloadCargo = false;
        }

        public void BeginPlacement(ITablePrefab cargo)
        {
            cargoVolumeView.SetVolume(_prefabVolumes.GetProfile(cargo).GetVolume(), Vector3Int.zero, _prefabVolumes.ParticleSize);   
        }
        public void EndPlacement()
        {
            cargoVolumeView.Clear();
            cargoVolumeView.transform.localPosition = _cargoVolumeViewInitialPosition;
        }

        public bool TryUnload(ITablePrefab cargo, Vector3 targetGroundPoint, Quaternion targetRotation, out PlaceCargoHandler handler)
        {
            _placeCounter++;
            if (_content.ContainsValue(cargo))
            {
                var profile = _prefabVolumes.GetProfile(cargo);
                IReadOnlyList<Vector3Int> volume = profile.GetVolume();
                Bounds bounds = profile.GetBounds();

                _unloadWorldPoint = targetGroundPoint - targetRotation * (bounds.min.y * 1.1f * Vector3.up);
                _unloadRotation = targetRotation;
                if (_visualizeUnloadCargo)
                {
                    cargoVolumeView.transform.position = _unloadWorldPoint;
                }
                Vector3 halfExtents = Vector3.one * (_prefabVolumes.ParticleSize * 0.5f);
                
                bool condition = true;
                foreach (var center in volume)
                {
                    Vector3 worldCellPoint = targetRotation * center * _prefabVolumes.ParticleSize + _unloadWorldPoint;
                    bool result = Physics.CheckBox(worldCellPoint, halfExtents, targetRotation, GameData.Data.cargoCheckLayer);
                    if (result)
                    {
                        condition = false;
                        if (_visualizeUnloadCargo)
                        {
                            cargoVolumeView.SetCollisionItem(center, 2);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (_visualizeUnloadCargo)
                        {
                            cargoVolumeView.SetCollisionItem(center, 0);
                        }
                    }
                }

                if (condition)
                {
                    _placeToken = _placeCounter;
                    _lastUnloadCandidatae = cargo;
                    handler = _unloadAcceptHandler;
                    return true;
                }
            }

            _lastUnloadCandidatae = null;
            handler = PlaceCargoHandler.Empty;
            return false;
        }

        private void UnloadLast()
        {
            if (_placeToken != _placeCounter)
            {
                Debug.LogError("CargoTrunk: PlaceToken was expired");
                return;
            }
            _placeToken = 0;

            var rb = DetachPrivate();
            rb.transform.position = _unloadWorldPoint;
            rb.transform.rotation = _unloadRotation;
        }

        private DynamicWorldObject DetachPrivate()
        {
            var rb = _lastUnloadCandidatae.transform.GetComponent<DynamicWorldObject>();
            rb.OnMassChanged -= OnCargoMassChanged;
            rb.ConvertToDynamic();
            int volumeAmount = _prefabVolumes.GetProfile(_lastUnloadCandidatae).GetVolume().Count;
            Vector3Int[] toRemove = new Vector3Int[volumeAmount];
            
            foreach (var kv in _content)
            {
                if (kv.Value == _lastUnloadCandidatae)
                {
                    toRemove[--volumeAmount] = kv.Key;
                }
            }
            foreach (var key in toRemove)
            {
                _content[key] = null;
            }

            _cargo.Remove(_lastUnloadCandidatae);
            return rb;
        }

        public void Detach(ITablePrefab cargo)
        {
            DetachPrivate();
        }

        public bool EnableInteraction => true;
        public Transform Root => transform;
        public bool RequestInteractive(ICharacterController character, out string data)
        {
            data = string.Empty;
            return true;
        }
    }
}