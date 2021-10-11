using System;
using Core.Structure;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Movement
{
    public class SmallSupport : Block, ISupport
    {
        [SerializeField] private Transform variatorTransform;

        
        [Header("Angles")]
        [SerializeField] private float pitchAngle;
        [SerializeField] private float rollAngle;

        [Header("Freedom")]
        [SerializeField] private bool xIsFree;
        [SerializeField] private bool yIsFree;
        [SerializeField] private bool zIsFree;

        [Header("Forces")]
        [Tooltip("Сила, при которой происходит срыв потока")]
        [SerializeField] private float disruptionForce;
        [Tooltip("Сила, при которой прекращается срыв потока")]
        [SerializeField] private float disruptionMinForce;
        [Tooltip("Сила возврата при расхождении q = 1м")]
        [SerializeField] private float returnForceMultiplier;
        [Tooltip("Сила остановки")]
        [SerializeField] private float dragForceMultiplier;
        [Tooltip("Максимальное отклонение")]
        [SerializeField] private float qMax;
        [Tooltip("Множитель силы при срыве"), Range(0, 1)]
        [SerializeField] private float disruptionForceMP;

        [Space(10)]

        [SerializeField] private Vector3 velocity;

        [SerializeField] private bool disruption;

        [Space(20), Header("Power")]
        [SerializeField] private float consumption;
        public Port<float> powerHandle = new Port<float>(PortType.Thurst);
        public AnimationCurve powerPerHandle;
        public PowerPort powerInput = new PowerPort();
        [ShowInInspector] private float power;

        public Port<float> pitch = new Port<float>(PortType.Thurst);
        public Port<float> roll = new Port<float>(PortType.Thurst);

        [ShowInInspector] private Vector3 p;
        private Vector3 lastPosition;
        private Vector3 force;
        private Vector3 deltaP;

        [ShowInInspector] private Vector2 variatorRotation;
        private Quaternion variator;

        private IDynamicStructure root;
        private float currentConsumption;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            if (structure is IDynamicStructure dynamicStructure) root = dynamicStructure;
        }

        public void ConsumptionTick()
        {
            currentConsumption = powerPerHandle.Evaluate(powerHandle.Value) * consumption;
            powerInput.charge = 0;
            powerInput.maxInput = currentConsumption;
            powerInput.maxOutput = 0;
        }
        public void PowerTick()
        {
            power = powerInput.charge / currentConsumption;
        }
        
        public void ApplyForce()
        {
            variatorRotation = new Vector2(Mathf.Clamp(pitch.Value, -1, 1) * pitchAngle * Mathf.Deg2Rad, Mathf.Clamp(roll.Value, -1, 1) * rollAngle * Mathf.Deg2Rad);
            variator = transform.rotation * Quaternion.AngleAxis(variatorRotation.x, Vector3.right) * Quaternion.AngleAxis(variatorRotation.y, Vector3.forward);
            Quaternion variator_i = Quaternion.Inverse(variator);
            
            
            deltaP = variator_i * (transform.position - lastPosition);
            velocity = deltaP / Time.deltaTime;
            velocity = new Vector3(velocity.x * (xIsFree ? 0f : 1f), velocity.y * (yIsFree ? 0f : 1f), velocity.z * (zIsFree ? 0f : 1f));
            velocity = variator * velocity;
            deltaP = new Vector3(deltaP.x * (xIsFree ? 1f : 0f), deltaP.y * (yIsFree ? 1f : 0f), deltaP.z * (zIsFree ? 1f : 0f));
            p += variator * deltaP;

            if (power == 0f)
            {
                p = transform.position;
                disruption = false;
            }

            var position = transform.position;
            p = variator_i * (p - position);
            p = new Vector3(p.x * (xIsFree ? 0f : 1f), p.y * (yIsFree ? 0f : 1f), p.z * (zIsFree ? 0f : 1f));
            p = (variator * p) + position;

            lastPosition = position;

            Vector3 deltaQ = (p - position).ClampDistance(0f, qMax * power);
            p = position + deltaQ;
            force = deltaQ * returnForceMultiplier / qMax;
            force -= velocity * (dragForceMultiplier * power);

            if (disruption == false)
            {
                if (force.magnitude > disruptionForce) disruption = true;
            }
            else
            {
                if (force.magnitude < disruptionMinForce) disruption = false;

                p = Vector3.Lerp(p, position, Time.deltaTime * qMax);
                force *= disruptionForceMP;
            }

            if (float.IsNaN(p.x))
                p = position;
            if (float.IsNaN(force.x))
                force = Vector3.zero;

            root.AddForce(force * Time.deltaTime, position);
        }
    }
}
