using Core.Structure;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Runtime.Structure.Rigging.Movement
{
    public class SmallSupport : Block, ISupport
    {
        [SerializeField] private Transform variatorTransform;

        [Header("Angles")] [SerializeField] private float pitchAngle;
        [SerializeField] private float rollAngle;

        [Header("Freedom")] [SerializeField] private bool xIsFree;
        [SerializeField] private bool yIsFree;
        [SerializeField] private bool zIsFree;

        [Header("Forces")] [Tooltip("Сила, при которой происходит срыв потока")] [SerializeField]
        private float disruptionForce;

        [Tooltip("Сила, при которой прекращается срыв потока")] [SerializeField]
        private float disruptionMinForce;

        [Tooltip("Сила возврата при расхождении q = qMax")] [SerializeField]
        private float returnForce;

        [Tooltip("Сила остановки")] [SerializeField]
        private float dragForce;

        [Tooltip("Максимальное отклонение")] [SerializeField]
        private float qMax;

        [Tooltip("Множитель силы при срыве"), Range(0, 1)] [SerializeField]
        private float disruptionForceMP;


        [Space(20), Header("Power")] [SerializeField]
        private float consumption;

        public Port<float> powerHandle = new Port<float>(PortType.Thurst);
        public AnimationCurve powerPerHandle;
        public PowerPort powerInput = new PowerPort();

        public Port<float> pitch = new Port<float>(PortType.Thurst);
        public Port<float> roll = new Port<float>(PortType.Thurst);
        
        [ShowInInspector, ReadOnly] private float power;
        [ShowInInspector, ReadOnly] private Vector3 p;
        private Vector3 lastPosition;
        private Vector3 force;
        private Vector3 deltaP;

        [Space(10)] [ShowInInspector, ReadOnly]
        private Vector3 velocity;

        [ShowInInspector, ReadOnly] private bool disruption;
        [ShowInInspector, ReadOnly] private Vector2 variatorRotation;
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
            currentConsumption = powerPerHandle.Evaluate(powerHandle.Value) * consumption * Time.deltaTime;
            powerInput.charge = 0;
            powerInput.maxInput = currentConsumption;
            powerInput.maxOutput = 0;
        }

        public void PowerTick()
        {
            if (currentConsumption == 0) power = 0;
            else power = powerInput.charge / currentConsumption;
        }

        public void ApplyForce()
        {
            variatorRotation = new Vector2(Mathf.Clamp(pitch.Value, -1, 1) * pitchAngle * Mathf.Deg2Rad,
                Mathf.Clamp(roll.Value, -1, 1) * rollAngle * Mathf.Deg2Rad);
            variator = transform.rotation * Quaternion.AngleAxis(variatorRotation.x, Vector3.right) *
                       Quaternion.AngleAxis(variatorRotation.y, Vector3.forward);
            Quaternion variator_i = Quaternion.Inverse(variator);


            deltaP = variator_i * (transform.position - lastPosition);
            velocity = deltaP / Time.deltaTime;
            velocity = new Vector3(velocity.x * (xIsFree ? 0f : 1f), velocity.y * (yIsFree ? 0f : 1f),
                velocity.z * (zIsFree ? 0f : 1f));
            velocity = variator * velocity;
            deltaP = new Vector3(deltaP.x * (xIsFree ? 1f : 0f), deltaP.y * (yIsFree ? 1f : 0f),
                deltaP.z * (zIsFree ? 1f : 0f));
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
            force = deltaQ * returnForce / qMax;
            force -= velocity * (dragForce * power);

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

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!variatorTransform) return;
            var localToWorldMatrix = variatorTransform.localToWorldMatrix;
            Handles.matrix = localToWorldMatrix;
            int freedom = 0;
            if (xIsFree) freedom++;
            if (yIsFree) freedom++;
            if (zIsFree) freedom++;

            if (freedom == 2)
            {
                Vector3 normal = Vector3.zero;
                Vector3 u = Vector3.zero;
                if (!xIsFree)
                {
                    normal = Vector3.right;
                    u = Vector3.up;
                }
                else if (!yIsFree)
                {
                    normal = Vector3.up;
                    u = Vector3.back;
                }
                else if (!zIsFree)
                {
                    normal = Vector3.forward;
                    u = Vector3.up;
                }

                Handles.CircleHandleCap(0, Vector3.zero, Quaternion.LookRotation(normal, u), qMax, EventType.Repaint);
                Handles.CircleHandleCap(0, normal * qMax, Quaternion.LookRotation(normal, u), qMax * 0.15f,
                    EventType.Repaint);
                Handles.CircleHandleCap(0, -normal * qMax, Quaternion.LookRotation(normal, u), qMax * 0.15f,
                    EventType.Repaint);
                Handles.DrawLine(-normal * qMax, normal * qMax);
            }

            if (!Application.isPlaying)
            {
                return;
            }

            Gizmos.DrawWireSphere(transform.position, 0.1f);
            Gizmos.color = disruption ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, p);
            Gizmos.DrawWireSphere(p, 0.1f);
        }
#endif
    }
}
