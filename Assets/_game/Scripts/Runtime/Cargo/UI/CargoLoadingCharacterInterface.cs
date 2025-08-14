using System;
using Core.Cargo;
using Core.Character;
using Core.Character.Interface;
using Core.Patterns.State;
using Core.Structure.Rigging.Cargo;
using Core.UiStructure;
using Core.UIStructure.Utilities;
using Core.Utilities;
using Runtime.Cargo.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Runtime.Cargo.UI
{
    public class CargoLoadingCharacterInterface : MonoBehaviour, IFirstPersonInterface, ISelectionListener<CargoButton>, ISelectionListener<TrunkButton>
    {
        [SerializeField] private CargoButton cargoButtonPrefab;
        [SerializeField] private TrunkButton trunkButtonPrefab;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button exitButton;
        private CargoPlacementInput _placementInput;
        private ListSelectionHandler<CargoButton> _cargoSelection = new(); 
        private ListSelectionHandler<TrunkButton> _trunkSelection = new(); 
        private ICargoLoadingPlayerHandler _handler;
        private FirstPersonController.UIInteractionState _interactionState;
        private FirstPersonInterfaceInstaller _master;
        private bool _isInPlacementMode;

        private void Awake()
        {
            _placementInput = new CargoPlacementInput();
            _placementInput.WASD.Cancel.performed += OnPressCancel;
            _placementInput.WASD.Confirm.performed += OnPressConfirm;
            _placementInput.WASD.Movement.performed += OnMovement;
            _placementInput.Disable();
        }

        public void Init(FirstPersonInterfaceInstaller master)
        {
            _master = master;
            _interactionState = ((FirstPersonController.UIInteractionState)_master.TargetState);
            _handler = (ICargoLoadingPlayerHandler)_interactionState.Handler;
            exitButton.onClick.AddListener(OnExitClick);
            _cargoSelection.AddListener(this);
            _trunkSelection.AddListener(this);
            //loadButton.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _placementInput.WASD.Cancel.performed -= OnPressCancel;
            _placementInput.WASD.Confirm.performed -= OnPressConfirm;
            _placementInput.WASD.Movement.performed -= OnMovement;
            _placementInput.Dispose();
            _cargoSelection.Dispose();
            _trunkSelection.Dispose();
            //loadButton.onClick.RemoveListener(OnClick);
        }

        public bool IsMatch(IState state)
        {
            return state is FirstPersonController.UIInteractionState { Handler: ICargoLoadingPlayerHandler };
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _handler.Enter();
            foreach (var trunk in _handler.AvailableTrunks)
            {
                var button = DynamicPool.Instance.Get(trunkButtonPrefab, transform);
                button.SetTarget((ICargoTrunkPlayerInterface)trunk);
                button.SetTrackingTarget(trunk.transform);
                _trunkSelection.AddTarget(button);
            }
            foreach (var cargo in _handler.AvailableCargo)
            {
                var button = DynamicPool.Instance.Get(cargoButtonPrefab, transform);
                button.SetTarget(cargo);
                button.SetTrackingTarget(cargo.transform);
                _cargoSelection.AddTarget(button);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _handler?.Exit();
            foreach (var target in _cargoSelection.Targets)
            {
                DynamicPool.Instance.Return(target);
            }
            foreach (var target in _trunkSelection.Targets)
            {
                DynamicPool.Instance.Return(target);
            }
            _cargoSelection.ClearTargets();
            _trunkSelection.ClearTargets();   
        }

        private void OnExitClick()
        {
            if (_isInPlacementMode)
            {
                ExitPlacement();
            }
            else
            {
                _interactionState.LeaveState();
            }
        }

        public void OnSelectionChanged(CargoButton prev, CargoButton next)
        {
            OnSelectionChanged();
        }
        public void OnSelectionChanged(TrunkButton prev, TrunkButton next)
        {
            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            if (_cargoSelection.Selected && _trunkSelection.Selected)
            {
                _isInPlacementMode = true;
                _trunkSelection.Selected.Data.EnterPlacement(_cargoSelection.Selected.Data);
                _placementInput.Enable();

                foreach (var target in _cargoSelection.Targets)
                {
                    target.gameObject.SetActive(false);
                }
                foreach (var target in _trunkSelection.Targets)
                {
                    target.gameObject.SetActive(false);
                }
            }
        }
        
        private void OnPressCancel(InputAction.CallbackContext obj)
        {
            ExitPlacement();
        }
        private void OnPressConfirm(InputAction.CallbackContext obj)
        {
            if (_trunkSelection.Selected.Data.Confirm())
            {
                ExitPlacement();
            }
        }

        private void ExitPlacement()
        {
            _isInPlacementMode = false;
            _trunkSelection.Selected.Data.ExitPlacement();
            _placementInput.Disable();
            foreach (var target in _trunkSelection.Targets)
            {
                target.gameObject.SetActive(true);
            }
            foreach (var target in _cargoSelection.Targets)
            {
                target.gameObject.SetActive(true);
            }
        }
        
        private void OnMovement(InputAction.CallbackContext obj)
        {
            var direction = obj.ReadValue<Vector2>();
            _trunkSelection.Selected.Data.Move(new Vector3Int(Mathf.RoundToInt(direction.x), 0, Mathf.RoundToInt(direction.y)));
        }
    }
    
}