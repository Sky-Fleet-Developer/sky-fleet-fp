using Core.Cargo;
using Core.Character;
using Core.Character.Interface;
using Core.Data;
using Core.Patterns.State;
using Core.Structure.Rigging.Cargo;
using Core.UIStructure.Utilities;
using Core.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Cargo.UI
{
    public class CargoUnloadingCharacterInterface : FirstPersonInterfaceBase, ISelectionListener<CargoButton>
    {
        [SerializeField] private CargoButton cargoButtonPrefab;
        [SerializeField] private Button exitButton;
        private ListSelectionHandler<CargoButton> _cargoSelection = new();
        private ICargoUnloadingPlayerHandler _handler;
        private FirstPersonController.UIInteractionState _interactionState;
        private bool _isPlacementMode;
        private CargoButton _currentSelection;
        private PlaceCargoHandler _placeCargoHandler;

        public override bool IsMatch(IState state)
        {
            return state is FirstPersonController.UIInteractionState { Handler: ICargoUnloadingPlayerHandler };
        }
        
        public override void Init(FirstPersonInterfaceInstaller master)
        {
            base.Init(master);
            _interactionState = ((FirstPersonController.UIInteractionState)Master.TargetState);
            _handler = (ICargoUnloadingPlayerHandler)_interactionState.Handler;
            exitButton.onClick.AddListener(OnExitClick);
            _cargoSelection.AddListener(this);
        }

        private void OnDestroy()
        {
            _cargoSelection.Dispose();
        }

        public override void Show()
        {
            base.Show();
            gameObject.SetActive(true);
            _handler.Enter();
            foreach (var cargo in _handler.AvailableCargo)
            {
                var button = DynamicPool.Instance.Get(cargoButtonPrefab, transform);
                button.SetTarget(cargo);
                button.SetTrackingTarget(cargo.transform);
                _cargoSelection.AddTarget(button);
            }
        }

        public override void Hide()
        {
            base.Hide();
            gameObject.SetActive(false);
            _handler?.Exit();
            foreach (var target in _cargoSelection.Targets)
            {
                DynamicPool.Instance.Return(target);
            }
            _cargoSelection.ClearTargets();
        }


        private void Update()
        {
            if (_isPlacementMode)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit raycastHit, 6, GameData.Data.walkableLayer))
                {
                    if (_handler.TryUnload(_currentSelection.Data, raycastHit.point,
                            Quaternion.LookRotation(raycastHit.normal, _currentSelection.Data.transform.forward) *
                            Quaternion.Euler(90, 0, 0), out _placeCargoHandler))
                    {
                        if (Input.GetKeyDown(KeyCode.Return))
                        {
                            _placeCargoHandler.PlaceAction?.Invoke();
                            var selection = _currentSelection;
                            _cargoSelection.RemoveTarget(selection);
                            DynamicPool.Instance.Return(selection);
                        }
                    }
                }
            }
        }

        public void OnSelectionChanged(CargoButton prev, CargoButton next)
        {
            if (next && (next != _currentSelection || !_currentSelection))
            {
                _currentSelection = next;
                _handler.BeginPlacement(next.Data);
                _isPlacementMode = true;
            }
            else
            {
                _currentSelection = null;
                _handler.EndPlacement();
                _isPlacementMode = false;
            }
        }
        
        private void OnExitClick()
        {
            _isPlacementMode = false;
            _interactionState.LeaveState();
        }
    }
}