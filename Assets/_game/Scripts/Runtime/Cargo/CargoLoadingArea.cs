using System.Collections.Generic;
using Core;
using Core.Cargo;
using Core.Character;
using Core.Configurations;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Cargo;
using Runtime.Trading;
using Unity.Cinemachine;
using UnityEngine;

namespace Runtime.Cargo
{
    public class TrunkLink
    {
        public IStructure Structure;
        public ICargoTrunk[] Trunks;
        public int Counter;
    }

    public class CargoLoadingArea : Block, IInteractiveObject, ICargoLoadingPlayerHandler
    {
        [SerializeField] private CinemachineCamera viewCamera;
        private readonly Dictionary<IStructure, TrunkLink> _detectedTrunks = new();
        private readonly Dictionary<ITablePrefab, int> _detectedCargo = new();
        private Queue<Collider> _enterAtStart = new();
        private bool _isInitialized;

        public IEnumerable<ITablePrefab> AvailableCargo => _detectedCargo.Keys;
        public bool EnableInteraction => IsActive;
        public Transform Root => transform;

        public IEnumerable<ICargoTrunk> AvailableTrunks
        {
            get
            {
                foreach (var detectedTrunksValue in _detectedTrunks.Values)
                {
                    foreach (var trunk in detectedTrunksValue.Trunks)
                    {
                        yield return trunk;
                    }
                }
            }
        }

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            Bootstrapper.OnLoadComplete.Subscribe(OnLoadComplete);
        }

        private void OnLoadComplete()
        {
            Structure.OnInitComplete.Subscribe(Dequeue);
        }

        private void Dequeue()
        {
            _isInitialized = true;
            while (_enterAtStart.Count > 0)
            {
                ProcessTriggerEnter(_enterAtStart.Dequeue());
            }
        }

        public bool TryLoad(ITablePrefab cargo, ICargoTrunk trunk, Vector3Int position)
        {
            if (trunk.TryPlaceCargo(cargo, position, out var handler))
            {
                handler.PlaceAction?.Invoke();
                return true;
            }
            return false;
        }
        
        public bool RequestInteractive(ICharacterController character, out string data)
        {
            data = "Cargo loading service";
            return true;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized)
            {
                _enterAtStart.Enqueue(other);
            }
            else
            {
                ProcessTriggerEnter(other);
            }
        }

        private void ProcessTriggerEnter(Collider other)
        {
            if (!other || !other.attachedRigidbody)
            {
                return;
            }
            if (other.attachedRigidbody.gameObject.TryGetComponent(out IStructure structure))
            {
                structure.OnInitComplete.Subscribe(ProcessStructure);
            }
            else if(other.attachedRigidbody.gameObject.TryGetComponent(out ITablePrefab tablePrefab))
            {
                if (_detectedCargo.TryGetValue(tablePrefab, out int counter))
                {
                    _detectedCargo[tablePrefab] = counter + 1;
                }
                else
                {
                    _detectedCargo.Add(tablePrefab, 1);
                }
            }
            
            void ProcessStructure()
            {
                var trunks = structure.GetBlocksByType<ICargoTrunk>();
                if (trunks.Length > 0)
                {
                    if (!_detectedTrunks.TryGetValue(structure, out var link))
                    {
                        _detectedTrunks.Add(structure, new TrunkLink { Structure = structure, Trunks = trunks, Counter = 1});
                    }
                    else
                    {
                        link.Counter++;
                    }
                }
            }
        }


        private void OnTriggerExit(Collider other)
        {
            if (other.attachedRigidbody.gameObject.TryGetComponent(out IStructure structure))
            {
                if (_detectedTrunks.TryGetValue(structure, out var link))
                {
                    link.Counter--;
                    if (link.Counter == 0)
                    {
                        _detectedTrunks.Remove(structure);
                    }
                }
            }
            else if (other.attachedRigidbody.gameObject.TryGetComponent(out ITablePrefab tablePrefab))
            {
                if (_detectedCargo.TryGetValue(tablePrefab, out int counter))
                {
                    if (counter == 1)
                    {
                        _detectedCargo.Remove(tablePrefab);
                    }
                    else
                    {
                        _detectedCargo[tablePrefab] = counter - 1;
                    }
                }
            }

        }

        void ICargoLoadingPlayerHandler.Enter()
        {
            viewCamera.gameObject.SetActive(true);   
        }

        void ICargoLoadingPlayerHandler.Exit()
        {
            viewCamera.gameObject.SetActive(false);
        }
    }
}