using System.Collections;
using Cinemachine;
using Core.Character;
using Core.Environment;
using Core.Game;
using Core.Patterns.State;
using Core.Structure;
using Core.Structure.Rigging;
using Core.SessionManager.GameProcess;
using Core.Structure.Rigging.Control;
using Core.GameSetting;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;


namespace Runtime.Character.Control
{
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
        [FoldoutGroup("Input")] public float verticalSpeed;
        [FoldoutGroup("Input")] public float horizontalSpeed;
        [FoldoutGroup("View")] public float horizontalBorders;
        [FoldoutGroup("View")] public float verticalBorders;

        public IControl AttachedControl => attachedControl;
        private IControl attachedControl;

        public State CurrentState { get; set; }
        //public Quaternion globalView;

        public readonly InteractionRaycast Interaction = new InteractionRaycast();

        private float vertical;
        
        private bool isInitialized;

        private InputButtons moveForward;
        private InputButtons moveBack;
        private InputButtons moveLeft;
        private InputButtons moveRight;
        private InputButtons jump;

        private void Start()
        {
            if (!isInitialized) Init();
        }

        public void Init()
        {
            CurrentState = new FreeWalkState(this);
            isInitialized = true;
            moveForward = (InputButtons)(InputControl.Instance.GetInput("Move player", "Move forward"));
            moveBack = (InputButtons)(InputControl.Instance.GetInput("Move player", "Move back"));
            moveLeft = (InputButtons)(InputControl.Instance.GetInput("Move player", "Move left"));
            moveRight = (InputButtons)(InputControl.Instance.GetInput("Move player", "Move right"));
            jump = (InputButtons)(InputControl.Instance.GetInput("Move player", "Jump"));
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
            if (!PauseGame.Instance.IsPause)
            {
                RotateHead();
            }
        }

        private void Update()
        {
            if (PauseGame.Instance.IsPause) return;

            CurrentState.Update();
        }
        
        private class InteractionState : State<FirstPersonController>
        {
            public InteractionState(FirstPersonController master) : base(master)
            {
            }

            public override void Update()
            {
                Ray ray;
                if (CursorBehaviour.RotationLocked) ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                else ray = new Ray(Master.cameraRoot.position, Master.cameraRoot.forward);
                
                if (StructureRaycaster.Cast(ray, true,
                    GameData.Data.interactionDistance, GameData.Data.interactiveLayer,
                    out StructureHit hit)) //Master.Interaction.Cast(Master.cameraRoot, out IInteractiveBlock block)
                {
                    if(hit.InteractiveBlock == null) return;

                    if (Master.attachedControl == null)
                    {
                        (bool canInteractive, string data) request = hit.InteractiveBlock.RequestInteractive(Master);
                        if (request.canInteractive)
                        {
                            //TODO: write text to HUD
                            if (Input.GetButtonDown("Interaction"))
                            {
                                hit.InteractiveBlock.Interaction(Master);
                            }
                        }
                    }

                    if (!CursorBehaviour.RotationLocked) return;

                    if(hit.Device == null) return;
                    if (Input.GetKey(KeyCode.Mouse0))
                    {
                        if (hit.Device is ControlAxis controlAxis)
                        {
                            if (controlAxis.EnableInteraction)
                            {
                                Debug.Log(controlAxis.computerInput);
                                controlAxis.MoveMalueInteractive(Input.GetAxis("Mouse Y") * Time.deltaTime);
                            }
                        }
                    }
                }
            }
        }
        
        private class FreeWalkState : InteractionState
        {
            public FreeWalkState(FirstPersonController master) : base(master)
            {
            }
            
            public override void Update()
            {
                base.Update();
                if (!Master.CanMove) return;
                
                Master.Move();
            }
        }
        
        private class SeatState : InteractionState
        {
            public SeatState(FirstPersonController master) : base(master)
            {
            }
            
            public override void Update()
            {
                base.Update();
                if (Input.GetButtonDown("Interaction"))
                {
                    Master.attachedControl.LeaveControl(Master);
                }
            }
        }


        private void RotateHead()
        {
            if (CursorBehaviour.RotationLocked) return;
            
            vertical = Mathf.Clamp(vertical - Input.GetAxis("Mouse Y") * verticalSpeed * Time.deltaTime,
                -verticalBorders, verticalBorders);
            transform.Rotate(Vector3.up * (Input.GetAxis("Mouse X") * horizontalSpeed * Time.deltaTime));
            cameraRoot.localEulerAngles = Vector3.right * vertical;
        }

        private void Move()
        {
            motor.InputAxis = new Vector2(InputControl.Instance.GetButton(moveForward) - InputControl.Instance.GetButton(moveBack),
                InputControl.Instance.GetButton(moveRight) - InputControl.Instance.GetButton(moveLeft));
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

    public class InteractionRaycast
    {
        public RaycastHit Hit;

        public bool Cast(Transform origin, out IInteractiveBlock block)
        {
            if (Physics.Raycast(origin.position, origin.forward, out Hit, GameData.Data.interactionDistance,
                GameData.Data.interactiveLayer))
            {
                block = Hit.collider.transform.GetComponentInParent<IInteractiveBlock>();
                return true;
            }

            block = null;
            return false;
        }

        public void CastControl(Transform cameraRoot, IControl attachedControl)
        {
            //attachedControl.CastControl()
        }
    }
}
