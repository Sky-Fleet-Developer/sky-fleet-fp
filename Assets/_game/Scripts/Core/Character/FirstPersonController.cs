using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Core.Cargo;
using Core.Character.Interaction;
using Core.Data;
using Core.Data.GameSettings;
using Core.Environment;
using Core.Game;
using Core.Items;
using Core.Patterns.State;
using Core.SessionManager.GameProcess;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using Core.Trading;
using DG.Tweening;
using Runtime;
using Runtime.Character;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Core.Character
{
    [RequireComponent(typeof(CharacterMotor))]
    public class FirstPersonController : MonoBehaviour, ICharacterController, IStateMaster, IInventoryOwner
    {
        [FoldoutGroup("Links")]
        public Transform cameraRoot;
        [FoldoutGroup("Links")]
        public CharacterMotor motor;
        [FoldoutGroup("Links")]
        public Rigidbody rigidbody;
        [FoldoutGroup("Links")]
        public Collider collider;
        
        [FoldoutGroup("Input")] public float verticalSpeed;
        [FoldoutGroup("Input")] public float horizontalSpeed;
        [FoldoutGroup("View")] public float horizontalBorders; // Not implemented
        [FoldoutGroup("View")] public float verticalBorders;
        [SerializeField] private CharacterDragObjectsSettings dragObjectsSettings;
        private NearObjectsScanner _nearObjectsScanner;
        
        public event Action StateChanged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!gameObject.activeInHierarchy) return;
            
            if (cameraRoot == null)
            {
                CinemachineVirtualCamera cam = GetComponentInChildren<CinemachineVirtualCamera>();
                if (cam) cameraRoot = cam.transform;
            }

            if (motor == null)
            {
                motor = GetComponent<CharacterMotor>();
            }

            if (rigidbody == null)
            {
                rigidbody = GetComponent<Rigidbody>();
            }

            if (collider == null)
            {
                collider = GetComponent<Collider>();
            }
        }
#endif

        public IDriveInterface AttachedIIDriveInterface => _attachedIIDriveInterface;
        private IDriveInterface _attachedIIDriveInterface;

        public IState CurrentState
        {
            get => currentInteractionState;
            set
            {
                currentInteractionState = (InteractionState)value;
                StateChanged?.Invoke();
            }
        }

        private InteractionState currentInteractionState;
        //public Quaternion globalView;

        private float vertical;
        
        private bool isInitialized;

        private InputButtons moveForward;
        private InputButtons moveBack;
        private InputButtons moveLeft;
        private InputButtons moveRight;
        private InputButtons jump;

        private ToggleSetting isAxisMove;
        private InputControl.CorrectInputAxis forwardAxis;
        private InputControl.CorrectInputAxis sideAxis;

        private InputControl.CorrectInputAxis cameraY;
        private InputControl.CorrectInputAxis cameraX;

        private void Awake()
        {
            currentInteractionState = new DefaultState(this);
            _nearObjectsScanner = new();
            WorldOffset.OnWorldOffsetChange += OnWorldOffsetChange;
        }

        private void Start()
        {
            if (!isInitialized) Init();
        }

        private void OnWorldOffsetChange(Vector3 offset)
        {
            currentInteractionState.OnWorldOffsetChange(offset);
        }

        public void Init()
        {
            CurrentState = new FreeWalkState(this);
            isInitialized = true;
            moveForward = InputControl.Instance.GetInput<InputButtons>("Move player", "Move forward");
            moveBack = InputControl.Instance.GetInput<InputButtons>("Move player", "Move back");
            moveLeft = InputControl.Instance.GetInput<InputButtons>("Move player", "Move left");
            moveRight = InputControl.Instance.GetInput<InputButtons>("Move player", "Move right");
            jump = InputControl.Instance.GetInput<InputButtons>("Move player", "Jump");

            isAxisMove = InputControl.Instance.GetInput<ToggleSetting>("Move player", "Use axles for move player?");
            forwardAxis = new InputControl.CorrectInputAxis();
            forwardAxis.SetAxis(InputControl.Instance.GetInput<InputAxis>("Move player", "Axis forward/back").GetAxis());
            sideAxis = new InputControl.CorrectInputAxis();
            sideAxis.SetAxis(InputControl.Instance.GetInput<InputAxis>("Move player", "Axis left/right").GetAxis());

            cameraY = new InputControl.CorrectInputAxis();
            cameraY.SetAxis(InputControl.Instance.GetInput<InputAxis>("Camera", "Axis up/down").GetAxis());
            cameraX = new InputControl.CorrectInputAxis();
            cameraX.SetAxis(InputControl.Instance.GetInput<InputAxis>("Camera", "Axis left/right").GetAxis());
        }

        private bool CanMove
        {
            get => motor.enabled;
            set
            {
                motor.enabled = value;
                motor.ResetPlatform();
                rigidbody.isKinematic = !value;
            }
        }

        private void LateUpdate()
        {
            if (PauseGame.Instance.IsPause) return;
            if (!CursorBehaviour.RotationLocked)
            {
                RotateHead();
            }
            currentInteractionState.LateUpdate();
        }

        private void Update()
        {
            if (PauseGame.Instance.IsPause) return;
            CurrentState.Run();
        }

        private void FixedUpdate()
        {
            currentInteractionState.FixedUpdate();
        }

        private class DefaultState : InteractionState
        {
            public DefaultState(FirstPersonController master) : base(master)
            {
            }
            public override void LateUpdate()
            {
            }
            public override void OnWorldOffsetChange(Vector3 offset)
            {
                if (!Master.CanMove) return;
                Master.motor.MoveOffset(offset);
                base.OnWorldOffsetChange(offset);
            }
        }

        public class InteractionState : IState<FirstPersonController>
        {
            public FirstPersonController Master { get; }
            public InteractionState(FirstPersonController master)
            {
                Master = master;
            }

            public virtual void OnWorldOffsetChange(Vector3 offset)
            {
                /*var cam = CinemachineBrain.SoloCamera;
                cam.OnTargetObjectWarped(Master.cameraRoot, offset);*/
            }

            public virtual void FixedUpdate() {}

            public virtual void LateUpdate()
            {
                Ray ray;
                if (CursorBehaviour.RotationLocked) ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                else ray = new Ray(Master.cameraRoot.position, Master.cameraRoot.forward);
                
                if (StructureRaycaster.Cast(ray, true, out StructureHit hit))
                {
                    if (hit.CharacterHandler == null)
                    {
                        if (hit.InteractiveObject is IInteractiveDynamicObject interactiveDynamicObject && Master._attachedIIDriveInterface == null)
                        {
                            if (Input.GetButtonDown("Interaction") || Input.GetKeyDown(KeyCode.Mouse0))
                            {
                                if (interactiveDynamicObject.RequestInteractive(Master, out _))
                                {
                                    SwitchToDynamic(interactiveDynamicObject, hit.RaycastHit);
                                }
                            }
                        }
                        return;
                    }

                    if (Master._attachedIIDriveInterface == null)
                    {
                        if (hit.InteractiveObject.RequestInteractive(Master, out _))
                        {
                            //TODO: write text to HUD
                            if (Input.GetButtonDown("Interaction"))
                            {
                                Master.EnterHandler(hit.CharacterHandler);
                                return;
                            }
                        }
                    }

                    if (!CursorBehaviour.RotationLocked) return;

                    if(hit.InteractiveObject == null) return;

                    if (Input.GetKey(KeyCode.Mouse0))
                    {
                        if (hit.InteractiveObject.EnableInteraction && hit.InteractiveObject is IInteractiveDevice device)
                        {
                            SwitchToDevice(device);
                            return;
                        }
                    }
                }
                /*Master._nearObjectsScanner.ScanThisFrame(Master.transform.position);
                float cosineA = 0.5f;
                IItemObject nearest = null;
                foreach (var itemObject in Master._nearObjectsScanner.GetResults<IItemObject>())
                {
                    float cosA = Vector3.Dot(ray.direction, (itemObject.transform.position - ray.origin).normalized);
                    if (cosA > cosineA)
                    {
                        cosineA = cosA;
                        nearest = itemObject;
                    }
                }

                if (nearest != null)
                {
                    if (Input.GetButtonDown("Interaction"))
                    {
                        
                    }
                }*/
            }

            public virtual void Run()
            {
                
            }

            private void SwitchToDevice(IInteractiveDevice device)
            {
                /*switch (device)
                {
                    case ControlAxis axis:
                        Debug.Log("Select device " + axis.computerInput);
                        Master.CurrentState = new ControlAxisState(Master, axis, this);                        
                        break;
                }*/
                Master.CurrentState = new ControlAxisState(Master, device, this);                        
            }

            private void SwitchToDynamic(IInteractiveDynamicObject interactiveDynamicObject, RaycastHit hitInfo)
            {
                Master.CurrentState = new InteractWithDynamicObjectState(interactiveDynamicObject, hitInfo, Master, this);                     

            }
        }
        
        public class InteractWithDynamicObjectState : FreeWalkState
        {
            private IInteractiveDynamicObject _target;
            private InteractionState _lastState;
            private RaycastHit _initialHitInfo;
            private Vector3 _localHitPoint;
            private float _distance;
            private Vector3 _wantedPoint;
            private Vector3 _currentPoint;
            private float _pullTension;
            public Vector3 WantedPoint => _wantedPoint;
            public Vector3 CurrentPoint => _currentPoint;
            public float PullTension => _pullTension;
            private bool _initialized;

            public InteractWithDynamicObjectState(IInteractiveDynamicObject target, RaycastHit initialHitInfo, FirstPersonController master, InteractionState lastState) : base(master)
            {
                _initialHitInfo = initialHitInfo;
                _lastState = lastState;
                _target = target;
                _localHitPoint = target.Rigidbody.transform.InverseTransformPoint(initialHitInfo.point);
                _distance = initialHitInfo.distance;
                _initialized = false;
            }

            public override void LateUpdate() { }
            public override void FixedUpdate()
            {
                if (!_initialized)
                {
                    return;
                }
                _currentPoint = _target.Rigidbody.transform.TransformPoint(_localHitPoint);
                Vector3 delta = _wantedPoint - _currentPoint;
                _pullTension = delta.magnitude;
                if (_pullTension > Master.dragObjectsSettings.disruptionDistance)
                {
                    Exit();
                    return;
                }

                if (!_target.ProcessPull(_pullTension))
                {
                    Exit();
                    return;
                }
                
                _pullTension /= Master.dragObjectsSettings.maxPullDistance;
                Vector3 alignForce = delta * (Master.dragObjectsSettings.maxPullForce * Master.dragObjectsSettings.pullCurve.Evaluate(Mathf.Max(_pullTension, 1)));
                Vector3 pullUpForce = Mathf.Min(_target.Rigidbody.mass * 9.81f * Master.dragObjectsSettings.pullUpRate, Master.dragObjectsSettings.maxPullForce * 0.5f) * Vector3.up;
                Vector3 breakForce = (Master.rigidbody.velocity - _target.Rigidbody.velocity) * (_target.Rigidbody.mass * Master.dragObjectsSettings.breakForceRate);
                if (Vector3.Dot(breakForce, alignForce) < 0)
                {
                    breakForce *= 0.2f;
                }
                Vector3 commonForce = alignForce + breakForce + pullUpForce;
                float forceRate = Mathf.Min(commonForce.magnitude, Master.dragObjectsSettings.maxPullForce);
                commonForce = commonForce.normalized * forceRate;
                if (_target.MoveTransitional)
                {
                    _target.Rigidbody.AddForce(commonForce);
                }
                else
                {
                    _target.Rigidbody.AddForceAtPosition(commonForce, _currentPoint);
                }
                Master.rigidbody.AddForce(-commonForce);
            }

            public override void Run()
            {
                base.Run();
                if (!Input.GetButton("Interaction") && !Input.GetKey(KeyCode.Mouse0))
                {
                    Exit();
                    return;
                }
                
                Ray ray;
                if (CursorBehaviour.RotationLocked) ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                else ray = new Ray(Master.cameraRoot.position, Master.cameraRoot.forward);

                _wantedPoint = ray.GetPoint(_distance);
                _initialized = true;
            }

            private void Exit()
            {
                Master.CurrentState = _lastState;
                _lastState = null;
                _target = null;
                _initialHitInfo = default;
            }
        }
        
        private class ControlAxisState : InteractionState
        {
            private InteractionState lastState;
            private IInteractiveDevice _device;

            public ControlAxisState(FirstPersonController master, IInteractiveDevice device, InteractionState lastState) : base(master)
            {
                _device = device;
                this.lastState = lastState;
            }

            public override void LateUpdate()
            {
            }

            public override void Run()
            {
                _device.MoveValueInteractive(Input.GetAxis("Mouse Y") * Time.deltaTime);

                if (!Input.GetKey(KeyCode.Mouse0))
                {
                    _device.ExitControl();
                    _device = null;
                    Master.CurrentState = lastState;
                    lastState = null;
                }
            }
        }
        
        public class FreeWalkState : InteractionState
        {

            public FreeWalkState(FirstPersonController master) : base(master)
            {
            }
            
            public override void OnWorldOffsetChange(Vector3 offset)
            {
                if (!Master.CanMove)
                {
                    Debug.Log($"FIRST_PERSON_CONTROLLER: world offset blocked");
                    return;
                }
                Master.motor.MoveOffset(offset);
                Debug.Log($"FIRST_PERSON_CONTROLLER: Moved by world offset to {Master.transform.position}");
                base.OnWorldOffsetChange(offset);
            }
            public override void Run()
            {
                base.Run();
                if (!Master.CanMove) return;
                
                Master.Move();
            }
        }
        
        public class UIInteractionState : InteractionState
        {
            private InteractionState _prevState;
            private ICharacterHandler _handler;
            private bool _canMove;
            private bool _rotationLocked;
            public ICharacterHandler Handler => _handler;

            public UIInteractionState(FirstPersonController master, InteractionState prevState, ICharacterHandler handler) : base(master)
            {
                _handler = handler;
                _prevState = prevState;
                _canMove = Master.CanMove;
                Master.CanMove = false;
                _rotationLocked = CursorBehaviour.RotationLocked;
                CursorBehaviour.UnlockCursor();
                CursorBehaviour.RotationLocked = true;
            }

            public void LeaveState()
            {
                CursorBehaviour.RotationLocked = _rotationLocked;
                CursorBehaviour.LockCursor();
                Master.CanMove = _canMove;
                Master.CurrentState = _prevState;
            }
        }
        
        private class SeatState : InteractionState
        {
            private IAimingInterface _aimingInterface;
            private bool _attachProcess;
            private InteractionState _prevState;

            public SeatState(FirstPersonController master, InteractionState prevState, IDriveInterface iIDriveInterface) : base(master)
            {
                _prevState = prevState;
                if (master._attachedIIDriveInterface is IAimingInterface aiming)
                {
                    _aimingInterface = aiming;
                }
                
                Master.StartCoroutine(AttachToControlRoutine(iIDriveInterface));
            }
            
            private IEnumerator AttachToControlRoutine(IDriveInterface iIDriveInterface)
            {
                _attachProcess = true;
                CharacterAttachData attachData = iIDriveInterface.GetAttachData();
                if (attachData.attachAndLock)
                {
                    Master.CanMove = false;
                    Master.transform.SetParent(attachData.anchor);
                    Master.collider.isTrigger = true;
                    attachData.transition.Setup(Vector3.zero, Master.transform.DOLocalMove);
                    yield return attachData.transition.Setup(Quaternion.identity, Master.transform.DOLocalRotateQuaternion).WaitForCompletion();
                }
                else
                {
                    attachData.transition.Setup(attachData.anchor.position, Master.transform.DOMove);
                    yield return attachData.transition.Setup(attachData.anchor.rotation, Master.transform.DORotateQuaternion).WaitForCompletion();
                }

                yield return new WaitForEndOfFrame();
                Master._attachedIIDriveInterface = iIDriveInterface;
                Master._attachedIIDriveInterface.OnCharacterEnter(Master);
                _attachProcess = false;
            }
            
            private IEnumerator LeaveControlRoutine()
            {
                var detachData = Master._attachedIIDriveInterface.GetDetachData();
                Master._attachedIIDriveInterface.OnCharacterLeave(Master);
                if (Master.CanMove)
                {
                    detachData.transition.Setup(detachData.anchor.position, Master.transform.DOMove);
                    yield return detachData.transition.Setup(detachData.anchor.rotation, Master.transform.DORotateQuaternion).WaitForCompletion();
                }
                else
                {
                    Master.transform.SetParent(detachData.anchor);
                    detachData.transition.Setup(Vector3.zero, Master.transform.DOLocalMove);
                    yield return detachData.transition.Setup(Quaternion.identity, Master.transform.DOLocalRotateQuaternion).WaitForCompletion();
                    Master.transform.SetParent(null);
                    Master.CanMove = true;
                    Master.collider.isTrigger = false;

                    Master.ScyncVelocity(Master._attachedIIDriveInterface.Structure);
                }
                Master._attachedIIDriveInterface = null;
                Master.CurrentState = _prevState;
            }
            
            public override void Run()
            {
                if (_attachProcess)
                {
                    return;
                }
                if (Input.GetButtonDown("Interaction"))
                {
                    Master.StartCoroutine(LeaveControlRoutine());
                    return;
                }

                if (_aimingInterface != null)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        Master.CurrentState = new SeatAimingState(Master, _aimingInterface, this);
                        CursorBehaviour.SetAimingState();
                        return;
                    }
                }
            }
        }

        private class SeatAimingState : InteractionState
        {
            protected IAimingInterface AimingInterface { get; private set; }
            private SeatState _lastState;
            private Vector2 _initialInput;
            private Vector2 _input;
            private AimingInterfaceState _initialAimingState;

            public SeatAimingState(FirstPersonController master, IAimingInterface aimingInterface, SeatState lastState) : base(master)
            {
                _lastState = lastState;
                AimingInterface = aimingInterface;
                _initialInput = AimingInterface.Input;
                _initialAimingState = AimingInterface.CurrentState;
                AimingInterface.SetState(AimingInterfaceState.Aiming);
            }

            public override void Run()
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    AimingInterface.SetState(_initialAimingState);
                    Master.CurrentState = _lastState;
                    CursorBehaviour.ExitAimingState();
                    return;
                }
                
                _input += new Vector2(Master.cameraX.GetInputAbsolute() * Master.horizontalSpeed * Time.deltaTime, -Master.cameraY.GetInputAbsolute() * Master.verticalSpeed * Time.deltaTime);
                AimingInterface.Input = _initialInput + _input;
            }
        }


        private void RotateHead()
        {
            float y;
            if(cameraY.IsAbsolute())
            {
                y = cameraY.GetInputAbsolute();
            }
            else
            {
                y = cameraY.GetInputSum();
            }
            float x;
            if (cameraX.IsAbsolute())
            {
                x = cameraX.GetInputAbsolute();
            }
            else
            {
                x = cameraX.GetInputSum();
            }
            vertical = Mathf.Clamp(vertical - y * verticalSpeed * Time.deltaTime,
                -verticalBorders, verticalBorders);
            transform.Rotate(Vector3.up * (x * horizontalSpeed * Time.deltaTime));
            cameraRoot.localEulerAngles = Vector3.right * vertical;
        }

        private void Move()
        {
            if (!isAxisMove.IsOn)
            {
                motor.InputAxis = new Vector2(InputControl.Instance.GetButton(moveForward) - InputControl.Instance.GetButton(moveBack),
                    InputControl.Instance.GetButton(moveRight) - InputControl.Instance.GetButton(moveLeft));
            }
            else
            {
                motor.InputAxis = new Vector2(forwardAxis.GetInputSum(), sideAxis.GetInputSum());
            }

            motor.InputSprint = Input.GetButton("Sprint");

            if (InputControl.Instance.GetButtonDown(jump) > 0)
            {
                motor.InputJump();
            }
            else if (InputControl.Instance.GetButtonUp(jump) > 0)
            {
                motor.InputCancelJump();
            }
        }

        public void EnterHandler(ICharacterHandler handler)
        {
            switch (handler)
            {
                case IDriveInterface characterInterface:
                    CurrentState = new SeatState(this, currentInteractionState, characterInterface);
                    break;
                case ITradeHandler: case ICargoLoadingHandler:
                    CurrentState = new UIInteractionState(this, currentInteractionState, handler);
                    break;
                case IPickUpHandler pickUpHandler:
                    pickUpHandler.PickUpTo(this);
                    break;
            }
        }
        

        private void ScyncVelocity(IStructure structure)
        {
            if (structure is IDynamicStructure dynamicStructure)
            {
                rigidbody.velocity = dynamicStructure.GetVelocityForPoint(transform.position);
            }
            else
            {
                rigidbody.velocity = Vector3.zero;
            }
        }

        public string InventoryKey => "Player_Inventory";
    }
}
