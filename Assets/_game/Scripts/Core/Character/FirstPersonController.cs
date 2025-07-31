using System;
using System.Collections;
using Cinemachine;
using Core.Data;
using Core.Data.GameSettings;
using Core.Environment;
using Core.Game;
using Core.Patterns.State;
using Core.SessionManager.GameProcess;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using DG.Tweening;
using Runtime;
using Runtime.Character;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Character
{
    [Serializable]
    public class CharacterDragObjectsSettings
    {
        public float maxPullForce;
        public float maxPullDistance;
        public float disruptionDistance;
        public AnimationCurve pullCurve;
        public float pullUpRate;
        public float breakForceRate;
    }
    
    [RequireComponent(typeof(CharacterMotor))]
    public class FirstPersonController : MonoBehaviour, ICharacterController, IStateMaster
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
        [FoldoutGroup("View")] public float horizontalBorders;
        [FoldoutGroup("View")] public float verticalBorders;
        [SerializeField] private CharacterDragObjectsSettings dragObjectsSettings;
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

        public IControl AttachedControl => attachedControl;
        private IControl attachedControl;

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
                
                if (StructureRaycaster.Cast(ray, true,
                    GameData.Data.interactionDistance, GameData.Data.interactiveLayer,
                    out StructureHit hit))
                {
                    if (hit.InteractiveBlock == null)
                    {
                        if (hit.InteractiveObject is IInteractiveDynamicObject interactiveDynamicObject && Master.attachedControl == null)
                        {
                            if (Input.GetButtonDown("Interaction") || Input.GetKeyDown(KeyCode.Mouse0))
                            {
                                (bool canInteract, string _) = interactiveDynamicObject.RequestInteractive(Master);
                                if (canInteract)
                                {
                                    SwitchToDynamic(interactiveDynamicObject, hit.RaycastHit);
                                }
                            }
                        }
                        return;
                    }

                    if (Master.attachedControl == null)
                    {
                        (bool canInteract, string _) = hit.InteractiveBlock.RequestInteractive(Master);
                        if (canInteract)
                        {
                            //TODO: write text to HUD
                            if (Input.GetButtonDown("Interaction"))
                            {
                                hit.InteractiveBlock.Interaction(Master);
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
                        }
                    }
                }
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
        
        private class SeatState : InteractionState
        {
            protected IAimingInterface AimingInterface { get; private set; }
            public SeatState(FirstPersonController master) : base(master)
            {
                if (master.attachedControl is IAimingInterface aiming)
                {
                    AimingInterface = aiming;
                }
            }
            public override void Run()
            {
                base.Run();
                if (Input.GetButtonDown("Interaction"))
                {
                    Master.attachedControl.LeaveControl(Master);
                    return;
                }

                if (AimingInterface != null)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        Master.CurrentState = new SeatAimingState(Master, this);
                        CursorBehaviour.SetAimingState();
                        return;
                    }
                }
            }
        }

        private class SeatAimingState : SeatState
        {
            private SeatState _lastState;
            private Vector2 _initialInput;
            private Vector2 _input;
            private AimingInterfaceState _initialAimingState;

            public SeatAimingState(FirstPersonController master, SeatState lastState) : base(master)
            {
                _lastState = lastState;
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


        public IEnumerator AttachToControl(IControl control)
        {
            CharacterAttachData attachData = control.GetAttachData();

            if (attachData.attachAndLock)
            {
                CanMove = false;
                transform.SetParent(attachData.anchor);
                collider.isTrigger = true;
                attachData.transition.Setup(Vector3.zero, transform.DOLocalMove);
                yield return attachData.transition.Setup(Quaternion.identity, transform.DOLocalRotateQuaternion).WaitForCompletion();
            }
            else
            {
                attachData.transition.Setup(attachData.anchor.position, transform.DOMove);
                yield return attachData.transition.Setup(attachData.anchor.rotation, transform.DORotateQuaternion).WaitForCompletion();
            }

            yield return new WaitForEndOfFrame();
            attachedControl = control;
            CurrentState = new SeatState(this);
        }

        public IEnumerator LeaveControl(CharacterDetachhData detachData)
        {
            if (CanMove)
            {
                detachData.transition.Setup(detachData.anchor.position, transform.DOMove);
                yield return detachData.transition.Setup(detachData.anchor.rotation, transform.DORotateQuaternion).WaitForCompletion();
            }
            else
            {
                transform.SetParent(detachData.anchor);
                detachData.transition.Setup(Vector3.zero, transform.DOLocalMove);
                yield return detachData.transition.Setup(Quaternion.identity, transform.DOLocalRotateQuaternion).WaitForCompletion();
                transform.SetParent(null);
                CanMove = true;
                collider.isTrigger = false;

                ScyncVelocity(attachedControl.Structure);
            }
            attachedControl = null;
            CurrentState = new FreeWalkState(this);
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
    }
}
