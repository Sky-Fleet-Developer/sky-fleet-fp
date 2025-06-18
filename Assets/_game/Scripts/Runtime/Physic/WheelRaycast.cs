using Core.Data;
using UnityEngine;

namespace Runtime.Physic
{
    public class WheelRaycast : MonoBehaviour
    {
        [Header("Общее")] 
        public float Radius;

        [Min(0.01f)] public float wheelMass = 20;
        [Header("Подвеска")] 
        public float SuspensionDisnatce = 1;
        [Header("сжатие / растяжение")]
        public Vector2 SuspensionDumping = new Vector2(1, 1);
        public float Spring = 500;
        public float Dumper = 100;
        [Header("Скольжение")] public float StaticFriction = 600;
        public float FrictionDrag = 200;
        [Min(0.0001f)] public float MaxFrictionOffset = 0.3f;
        [Min(0.01f)] public float SlidingValue = 0.5f;

        [Header("Сила срыва")] 
        public float MaxStaticFriction;
        [Header("Сила сцепления")] 
        public float MinSlidingFriction;

        [Header("Тормоз")] 
        public float BreakForce;

        [Header("Земля")] 
        public LayerMask groundLayer;

        private float Force;
        private float RPM;
        private float turns;
        public Vector3 localPosition { get; private set; }
        public Quaternion localRotation { get; private set; }

        
        private float CircleLength => Radius * Mathf.PI * 2;
        private bool sliding;
        private RaycastHit groundHit;
        private float suspension_distance;
        private float suspension_position;
        private float suspension_delta;
        private bool grounded;
        private Rigidbody rigidbody;
        private Vector3 PlatformPoint;
        private Vector3 worldVelocity;
        private float forwardDelta;
        private float forwardVelocity;
        private float sideDelta;
        private float sideVelocity;
        private Transform platform;

        private float current_spring;

        private float addRPM;

        private void Start()
        {
            rigidbody = GetComponentInParent<Rigidbody>();
            if (rigidbody == null) enabled = false;
        }

        private void FixedUpdate()
        {
            GroundCast(transform.position);
            DoSpring(Time.deltaTime);
            DoFriction(Time.deltaTime);
        }

        private void GroundCast(Vector3 position)
        {
            grounded = UnityEngine.Physics.Raycast(position, -transform.up, out groundHit, SuspensionDisnatce,
                GameData.Data.walkableLayer);
            Debug.DrawLine(position, position - transform.up * (grounded ? groundHit.distance : SuspensionDisnatce));
        }

        private void DoSpring(float deltaTime)
        {
            float lastD = suspension_distance;

            if (grounded)
                suspension_distance = groundHit.distance;
            else
                suspension_distance = SuspensionDisnatce;

            suspension_delta = Mathf.Clamp((suspension_distance - lastD) / deltaTime, -SuspensionDumping.x,
                SuspensionDumping.y);

            suspension_position = 1 - (lastD + suspension_delta * deltaTime) / SuspensionDisnatce;

            if (grounded)
            {
                current_spring = (suspension_position * Spring + Mathf.Clamp(-suspension_delta, 0, 1) * Dumper) * deltaTime;
                Vector3 impulse = groundHit.normal * current_spring;
                rigidbody.AddForceAtPosition(impulse, groundHit.point);
                Rigidbody otherRB = groundHit.rigidbody;
                if (otherRB != null)
                {
                    otherRB.AddForceAtPosition(-impulse, groundHit.point);
                }
            }
        }

        private void DoFriction(float deltaTime)
        {
            RPM *= (1 - BreakForce);

            if (grounded)
            {
                Vector3 fwd = Vector3.Cross(groundHit.normal, transform.right);

                Quaternion fwdDir = Quaternion.LookRotation(fwd, groundHit.normal);

                Vector3 lastPlatformPoint = PlatformPoint;
                
                if (Equals(lastPlatformPoint, Vector3.zero) || platform != groundHit.transform)
                {
                    platform = groundHit.transform;
                    PlatformPoint = platform.InverseTransformDirection(groundHit.point - platform.position);
                    return;
                }
                
                PlatformPoint = platform.InverseTransformDirection(groundHit.point - platform.position);
                
                //to world

                worldVelocity = platform.rotation * (PlatformPoint - lastPlatformPoint) / deltaTime;
                
                //to local

                Vector3 localVelocity = Quaternion.Inverse(fwdDir) * worldVelocity;

                //transforms

                float lastRpm = RPM;
                RPM += addRPM * deltaTime;
                addRPM = 0;

                forwardVelocity = localVelocity.z + RPM * CircleLength / 60;
                forwardDelta = Mathf.Clamp(forwardDelta + forwardVelocity * deltaTime, -MaxFrictionOffset,
                    MaxFrictionOffset);

                sideVelocity = localVelocity.x;
                sideDelta = Mathf.Clamp(sideDelta + sideVelocity * deltaTime, -MaxFrictionOffset, MaxFrictionOffset);

                //to world

                Vector3 worldDelta = fwdDir *
                                     (Vector3.right * sideDelta + Vector3.forward * forwardDelta); //world space
                worldVelocity = fwdDir *
                                (Vector3.right * sideVelocity + Vector3.forward * forwardVelocity);
                
                RPM -= (forwardVelocity * FrictionDrag + forwardDelta * StaticFriction) /
                    CircleLength * 60 / wheelMass * deltaTime;

                if (RPM > 0 != lastRpm > 0)
                {
                    float deltaRpm = RPM - lastRpm;
                    RPM = lastRpm + deltaRpm * 0.3f;
                }

                if (float.IsNaN(RPM)) RPM = 0;

                Vector3 force = (-worldVelocity * FrictionDrag - worldDelta * StaticFriction);

                float forceMagniture = force.magnitude;

                if (sliding)
                {
                    if (forceMagniture < MinSlidingFriction * current_spring)
                        sliding = false;
                    force *= SlidingValue;
                }
                else
                {
                    if (forceMagniture > MaxStaticFriction * current_spring)
                        sliding = true;
                }

                rigidbody.AddForceAtPosition(force * deltaTime, groundHit.point);
                Rigidbody otherRB = groundHit.rigidbody;
                if (otherRB != null)
                {
                    otherRB.AddForceAtPosition(-force * deltaTime, groundHit.point);
                }
            }
            else
            {
                sliding = false;
                sideVelocity = 0f;
                sideDelta = 0f;
                PlatformPoint = Vector3.zero;
            }

            localPosition = transform.InverseTransformPoint(groundHit.point) + Vector3.up * Radius;
            turns += RPM / 60 * deltaTime;
            localRotation = Quaternion.AxisAngle(Vector3.right, turns * Mathf.PI);
        }

        public void AddForce(float force)
        {
            addRPM += force / CircleLength * 60 / wheelMass;
        }
    }
}