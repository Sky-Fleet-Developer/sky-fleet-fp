using Management;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character
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
        [FoldoutGroup("Locomotor")] public float height = 1.8f;
        [FoldoutGroup("Locomotor")] public float skinWidth = 0.2f;
        [FoldoutGroup("Locomotor")] public float radius = 0.4f;
        [FoldoutGroup("Locomotor"),
         Header("Скольжение")] public float staticFriction = 600;
        [FoldoutGroup("Locomotor")] public float frictionDrag = 200;
        [FoldoutGroup("Locomotor"), Min(0.0001f)] public float maxFrictionOffset = 0.3f;
        [FoldoutGroup("Locomotor"), Min(0.01f)] public float slidingValue = 0.5f;
        [FoldoutGroup("Locomotor"),
         Header("Скольжение")] public float accelerationInclination = 0.01f;
        [FoldoutGroup("Locomotor"),
         Header("Скольжение")] public float inclinationMax = 1f;
        [FoldoutGroup("Locomotor"),
         Header("Скольжение")] public float inclinationHardness = 1f;

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
        private Vector3 platformPoint;
        private Vector3 worldVelocity;
        private Vector3 normalAcceleration;
        private Vector3 lastVelocity;
        private float forwardDelta;
        private float forwardVelocity;
        private float sideDelta;
        private float sideVelocity;
        private Transform platform;
        
        private Vector2 targetSpeed;
        private bool jump;
        private bool canJump = true;
        
        
        public bool InputSprint { get; set; }
        public Vector2 InputAxis { get; set; }

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
            float along = Mathf.Clamp(InputAxis.x, -1f, 1f) * (InputAxis.x > 0 ? (InputSprint ? sprintSpeed : forwardSpeed) : backSpeed);
            targetSpeed = new Vector2(along, sideSpeed * Mathf.Clamp(InputAxis.y, -1f, 1f));
            
        }

        private void GroundCast(Vector3 position)
        {
            grounded = Physics.SphereCast(position, radius, -transform.up, out groundHit, height + skinWidth - radius,
                GameData.Data.groundLayer);
            Debug.DrawLine(position, position - transform.up * (grounded ? groundHit.distance : height + skinWidth));
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
                
                platformPoint = platform.InverseTransformDirection(groundHit.point - platform.position);
                
                //to world

                worldVelocity = platform.rotation * (platformPoint - lastPlatformPoint) * idt;

                //to local

                Vector3 localVelocity = fwdInv * worldVelocity;

                //transforms

                forwardVelocity = localVelocity.z + targetSpeed.x;
                forwardDelta = Mathf.Clamp(forwardDelta + forwardVelocity * deltaTime, -maxFrictionOffset,
                    maxFrictionOffset);

                sideVelocity = localVelocity.x + targetSpeed.y;
                sideDelta = Mathf.Clamp(sideDelta + sideVelocity * deltaTime, -maxFrictionOffset, maxFrictionOffset);

                //to world

                Vector3 worldDelta = fwdDir *
                                     (Vector3.right * sideDelta + Vector3.forward * forwardDelta); //world space
                worldVelocity = fwdDir *
                                (Vector3.right * sideVelocity + Vector3.forward * forwardVelocity);
                
                
                Vector3 force = (-worldVelocity * frictionDrag - worldDelta * staticFriction);
                
                
                //acceleration
                Vector3 acceleration = (lastVelocity - rigidbody.velocity) * idt;
                lastVelocity = rigidbody.velocity;
                normalAcceleration = Vector3.ProjectOnPlane(acceleration, groundHit.normal);
                
                float forceMagniture = force.magnitude;

                if (sliding)
                {
                    if (forceMagniture < minSlidingFriction)
                        sliding = false;
                    force *= slidingValue;
                }
                else
                {
                    if (forceMagniture > maxStaticFriction)
                        sliding = true;
                }

                if (jump && canJump)
                {
                    canJump = false;
                    jump = false;
                    rigidbody.velocity += groundHit.normal * jumpImpulse;
                }

                rigidbody.AddForceAtPosition(force * deltaTime, groundHit.point);
                var otherRb = groundHit.rigidbody;
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
