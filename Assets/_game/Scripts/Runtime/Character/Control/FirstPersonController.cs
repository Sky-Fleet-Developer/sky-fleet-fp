using System.Collections;
using Cinemachine;
using Core.Character;
using Core.Structure;
using Core.Structure.Rigging;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Character.Control
{
    [RequireComponent(typeof(CharacterMotor))]
    public class FirstPersonController : MonoBehaviour, ICharacterController
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
        [ShowInInspector, FoldoutGroup("Links")]
        public string refresher
        {
            get
            {
                if (gameObject.activeInHierarchy)
                {
                    if (cameraRoot == null)
                    {
                        var cam = GetComponentInChildren<CinemachineVirtualCamera>();
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
                
                string val = "--";
                val = val.Insert(Time.frameCount % 3, "*");
                return val;
            }
        }
        #endif
        [FoldoutGroup("Input")] public float verticalSpeed;
        [FoldoutGroup("Input")] public float horizontalSpeed;
        [FoldoutGroup("View")] public float horizontalBorders;
        [FoldoutGroup("View")] public float verticalBorders;

        public IControl AttachedControl => attachedControl;
        private IControl attachedControl;

        
        //public Quaternion globalView;

        public readonly InteractionRaycast Interaction = new InteractionRaycast();
        
        private float vertical;

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
            RotateHead();
        }

        private void Update()
        {
            if (attachedControl != null)
            {
                if (Input.GetButtonDown("Interaction"))
                {
                    attachedControl.LeaveControl(this);
                }

                if (Input.GetButton("Fire1"))
                {
                    //Interaction.CastControl(cameraRoot, attachedControl);
                }
            }
            else if (CanMove)
            {
                Move();
                
                if (Interaction.Cast(cameraRoot, out var block))
                {
                    var request = block.RequestInteractive(this);
                    if (request.canInteractive)
                    {
                        //TODO: write text to HUD
                        if (Input.GetButtonDown("Interaction"))
                        {
                            block.Interaction(this);
                        }
                    }
                }
            }


        }
        

        private void RotateHead()
        {
            vertical = Mathf.Clamp(vertical - Input.GetAxis("Mouse Y") * verticalSpeed * Time.deltaTime,
                -verticalBorders, verticalBorders);
            transform.Rotate(Vector3.up * (Input.GetAxis("Mouse X") * horizontalSpeed * Time.deltaTime));
            cameraRoot.localEulerAngles = Vector3.right * vertical;
        }

        private void Move()
        {
            motor.InputAxis = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
            motor.InputSprint = Input.GetButton("Sprint");
            
            if (Input.GetButtonDown("Jump"))
            {
                motor.InputJump();
            }
            else if (Input.GetButtonUp("Jump"))
            {
                motor.InputCancelJump();
            }
        }


        public IEnumerator AttachToControl(IControl control)
        {
            var attachData = control.GetAttachData();
            
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