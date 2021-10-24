using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Character
{
    public class CharacterMotor : MonoBehaviour
    {
        
        //--------character properties--------//
        [FoldoutGroup("Character")] public float sprintSpeed;
        [FoldoutGroup("Character")]public float forwardSpeed;
        [FoldoutGroup("Character")]public float sideSpeed;
        [FoldoutGroup("Character")] public float backSpeed;
        [FoldoutGroup("Character")]public float jumpImpulse;
        
        //--------locomotor properties--------//
        [FoldoutGroup("Locomotor")] public float skinWidth = 0.2f;
        [FoldoutGroup("Locomotor")] public float radius = 0.4f;
        [FoldoutGroup("Locomotor"),
         Header("Скольжение")] public float staticFriction = 600;
        [FoldoutGroup("Locomotor")] public float frictionDrag = 200;
        [FoldoutGroup("Locomotor"), Min(0.0001f)] public float maxFrictionOffset = 0.3f;
        [FoldoutGroup("Locomotor"), Min(0.01f)] public float slidingValue = 0.5f;
        [FoldoutGroup("Locomotor"), Header("Наклоны")] public float accelerationInclination = 0.01f;
        [FoldoutGroup("Locomotor")] public float inclinationMax = 1f;
        [FoldoutGroup("Locomotor")] public float inclinationHardness = 1f;
        [FoldoutGroup("Locomotor"), Header("Препятствия")] public float rayPerInputOffset = 0.1f;
        [FoldoutGroup("Locomotor")] public float yDragMul = 2f;

        [Header("Сила срыва"), FoldoutGroup("Locomotor")] 
        public float maxStaticFriction;
        [Header("Сила сцепления"), FoldoutGroup("Locomotor")] 
        public float minSlidingFriction;
        
        //--------runtime--------//
        private bool sliding;
        private RaycastHit groundHit;
        private float suspensionDistance;
        private float suspensionPosition;
        private float suspensionDelta;
        private bool grounded;
        private Rigidbody rigidbody;
        private Vector3 lastGroundPoint;
        private Vector3 platformPoint;
        private Vector3 worldVelocity;
        private Vector3 normalAcceleration;
        private Vector3 lastPlatformVelocity;
        private float forwardDelta;
        private float forwardVelocity;
        private float sideDelta;
        private float sideVelocity;
        private Transform platform;
        
        private Vector2 targetSpeed;
        [ShowInInspector] private bool jump;
        [ShowInInspector, ReadOnly] private bool canJump = true;
        private bool jumpTickNow;

        
        [ShowInInspector, ReadOnly] public bool InputSprint { get; set; }
        [ShowInInspector, ReadOnly]  public Vector2 InputAxis { get; set; }
        private Vector2 input;

        public void InputJump()
        {
            jump = true;
        }

        public void InputCancelJump()
        {
            jump = false;
        }

        public void ResetPlatform()
        {
            platformPoint = Vector3.zero;
            platform = null;
        }
        
        private void Start()
        {
            rigidbody = GetComponentInParent<Rigidbody>();
            if (rigidbody == null) enabled = false;
        }

        private void FixedUpdate()
        {
            ReadInput();
            GroundCast(transform.position);
            DoFriction(Time.deltaTime);
            Inclination();
        }

        private void ReadInput()
        {
            input.x = Mathf.Lerp(input.x, Mathf.Clamp(InputAxis.x, -1f, 1f), Time.deltaTime * 2.5f);
            input.y = Mathf.Lerp(input.y, Mathf.Clamp(InputAxis.y, -1f, 1f), Time.deltaTime * 2.5f);
            
            float along = input.x * (input.x > 0 ? (InputSprint ? sprintSpeed : forwardSpeed) : backSpeed);
            targetSpeed = new Vector2(along, sideSpeed * Mathf.Clamp(input.y, -1f, 1f));
        }

        private void GroundCast(Vector3 position)
        {
            lastGroundPoint = groundHit.point;

            Vector3 offset = transform.forward * input.x + transform.right * input.y;

            position = position + transform.up * (radius + skinWidth) + offset * rayPerInputOffset;
            
            grounded = Physics.SphereCast(position, radius, -transform.up, out groundHit, skinWidth * 2,
                GameData.Data.groundLayer);
            
            //Debug.DrawLine(position, transform.position, Color.cyan);
            Debug.DrawRay(position, -transform.up * groundHit.distance, Color.cyan);
            
            //Debug.DrawLine(position, position - transform.up * (grounded ? groundHit.distance : height + skinWidth));
        }

        private void DoFriction(float deltaTime)
        {
            if (grounded)
            {
                float idt = 1f / deltaTime;
                Vector3 fwd = Vector3.Cross(groundHit.normal, transform.right);

                Quaternion fwdDir = Quaternion.LookRotation(fwd, groundHit.normal);
                Quaternion fwdInv = Quaternion.Inverse(fwdDir);

                Vector3 lastPlatformPoint = platformPoint;

                if (Equals(lastPlatformPoint, Vector3.zero) || platform != groundHit.transform)
                {
                    platform = groundHit.transform;
                    platformPoint = platform.InverseTransformDirection(groundHit.point - platform.position);
                    return;
                }

                Vector3 transposedPlatformPoint =
                    platform.InverseTransformDirection(lastGroundPoint - platform.position);
                platformPoint = platform.InverseTransformDirection(groundHit.point - platform.position);


                //acceleration

                Vector3 platformVelocity = (lastPlatformPoint - transposedPlatformPoint) * idt;
                Vector3 platformAcceleration = (lastPlatformVelocity - platformVelocity) * idt;
                lastPlatformVelocity = platformVelocity;
                normalAcceleration = Vector3.ProjectOnPlane(platformAcceleration, groundHit.normal);

                //to world

                worldVelocity = platform.rotation * (platformPoint - lastPlatformPoint) * idt;
                Vector3 selfVelocity = rigidbody.velocity - platformVelocity;

                //to local

                Vector3 localVelocity = fwdInv * worldVelocity;
                Vector3 localSelfVelocity = fwdInv * (selfVelocity);

                //transforms

                forwardVelocity = localVelocity.z + targetSpeed.x;
                forwardDelta = Mathf.Clamp(forwardDelta + forwardVelocity * deltaTime, -maxFrictionOffset,
                    maxFrictionOffset);

                sideVelocity = localVelocity.x + targetSpeed.y;
                sideDelta = Mathf.Clamp(sideDelta + sideVelocity * deltaTime, -maxFrictionOffset, maxFrictionOffset);

                jumpTickNow = false;
                if (jump && canJump)
                {
                    canJump = false;
                    this.Wait(0.5f, () => canJump = true);
                    jump = false;
                    jumpTickNow = true;
                }

                //apply skin width
                float yDelta = 0;
                float yVel = 0;
                if (canJump)
                {
                    yDelta = Mathf.Clamp(groundHit.distance - skinWidth * 1.5f, -skinWidth * 0.5f, skinWidth);
                    yVel = Mathf.Max(localSelfVelocity.y, -1) * rigidbody.mass;
                   // transform.position -= transform.up * yDelta;
                }
                //to world

                Vector3 worldDelta = fwdDir *
                                     new Vector3(sideDelta, yDelta * rigidbody.mass * 0.05f, forwardDelta); //world space
                worldVelocity = fwdDir *
                                new Vector3(sideVelocity, yVel * yDragMul, forwardVelocity);


                Vector3 force = (-worldVelocity * frictionDrag - worldDelta * staticFriction);

                float forceMagnitude = force.magnitude;

                if (sliding)
                {
                    if (forceMagnitude < minSlidingFriction)
                        sliding = false;
                    force *= slidingValue;
                }
                else
                {
                    if (forceMagnitude > maxStaticFriction)
                        sliding = true;
                }

                if (jumpTickNow)
                {
                    selfVelocity = Vector3.ProjectOnPlane(selfVelocity, groundHit.normal) +
                                   groundHit.normal * jumpImpulse;
                    rigidbody.velocity = selfVelocity;
                }

                rigidbody.AddForceAtPosition(force * deltaTime, groundHit.point);

                Debug.DrawRay(groundHit.point, force * 0.0001f, Color.red);
                //Debug.DrawRay(groundHit.point, groundHit.normal * 0.2f, Color.green);

                Rigidbody otherRb = groundHit.rigidbody;
                if (otherRb != null)
                {
                    otherRb.AddForceAtPosition(-force * deltaTime, groundHit.point);
                }
            }
            else
            {
                canJump = true;
                sliding = false;
                sideVelocity = 0f;
                sideDelta = 0f;
                platformPoint = Vector3.zero;
            }
        }

        private void Inclination()
        {
            Vector3 fwd = transform.forward;
            fwd.y = 0;
            float aMag = normalAcceleration.magnitude;
            Vector3 aDir = aMag == 0 ? Vector3.zero : normalAcceleration / aMag;
            Vector3 up = Vector3.up - aDir * (Mathf.Min(aMag * accelerationInclination, inclinationMax));
            
            Quaternion qUp = Quaternion.LookRotation(up, fwd);
            
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(qUp * Vector3.up, qUp * Vector3.forward), Time.deltaTime * inclinationHardness);
        }
    }
}
