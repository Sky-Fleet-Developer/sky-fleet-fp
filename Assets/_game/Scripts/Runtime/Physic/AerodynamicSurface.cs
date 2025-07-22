using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Runtime.Physic
{
    public class AerodynamicSurface : MonoBehaviour
    {
        // Геометрические параметры крыла
        [FormerlySerializedAs("frontEdgeLength")] public float farthestEdgeLength = 1f; // передняя кромка (длина нижнего основания)
        [FormerlySerializedAs("rearEdgeLength")] public float nearestEdgeLength = 2f; // задняя кромка (длина верхнего основания)
        public float height = 0.5f; // размах крыла (расстояние между двумя краями)

        public float spanwiseOffset = 0.1f; // смещение заднего края вперёд (для положительной высоты - вправо, для отрицательной - влево)

        public int numZones = 10; // количество зон разделения по высоте

        // Физические коэффициенты
        public float liftFactorFromForwardSpeed = 1f; // коэффициент подъёмной силы от продольной скорости

        public AnimationCurve angleOfAttackInfluenceCurve = new AnimationCurve(
            new Keyframe(-Mathf.PI / 2, -1), // ноль подъёмной силы при большом отрицательном угле атаки
            new Keyframe(Mathf.PI / 2, 1f)); // максимальная подъёмная сила при большем положительном угле атаки

        // Глобальные переменные
        private Rigidbody parentRigidbody;
        private List<AerodynamicZone> zones;
        private Vector3[] cachedCorners; // кэшируемые углы для рисования контура

        private void OnValidate()
        {
            cachedCorners = null;
            InitializeZones();
        }

        void Start()
        {
            parentRigidbody = GetComponentInParent<Rigidbody>();
            if (!parentRigidbody)
                Debug.LogError("RigidBody не найден в иерархии!");

            InitializeZones();
            cachedCorners = CalculateCorners(); // сохраняем вершины крыла один раз
        }

        private void InitializeZones()
        {
            zones = new List<AerodynamicZone>(EnumerateZones());
        }

        private IEnumerable<AerodynamicZone> EnumerateZones()
        {
            for (int i = 0; i < numZones; ++i)
            {
                float leftBorder = (float)i / numZones;
                float rightBorder = (i + 1.0f) / numZones;
                float mid = (i + 0.5f) / numZones;
                float leftWidth = Mathf.Lerp(nearestEdgeLength, farthestEdgeLength, leftBorder);
                float rightWidth = Mathf.Lerp(nearestEdgeLength, farthestEdgeLength, rightBorder);
                Vector3 localPosition = new Vector3(mid * height, 0f, spanwiseOffset * mid);
                float area = 0.5f * (leftWidth + rightWidth) * (Mathf.Abs(height) / (float)numZones);
                yield return new AerodynamicZone(localPosition, area);
            }
        }

        struct AerodynamicZone
        {
            public readonly Vector3 LocalPosition;
            public readonly float Area;

            public AerodynamicZone(Vector3 position, float area)
            {
                this.LocalPosition = position;
                this.Area = area;
            }
        }

        void FixedUpdate()
        {
            foreach (var zone in zones)
            {
                ComputeLift(zone);
            }
        }

        private void ComputeLift(AerodynamicZone zone)
        {
            // локальная скорость зоны относительно глобального движения объекта
            var relativeVelocity = parentRigidbody.GetPointVelocity(transform.TransformPoint(zone.LocalPosition)) -
                                   parentRigidbody.velocity;
            var localRelativeVelocity = transform.InverseTransformDirection(relativeVelocity);

            // компоненты скорости вдоль осей

            // угол атаки - угол между направлением потока воздуха и плоскостью крыла
            var angleOfAttack = Mathf.Asin(localRelativeVelocity.normalized.y);

            // коэффициент подъёмной силы зависит от угла атаки и продольной скорости
            var liftFactorByAngle = angleOfAttackInfluenceCurve.Evaluate(angleOfAttack);
            var liftForceMagnitude = -Mathf.Abs(localRelativeVelocity.y) * liftFactorFromForwardSpeed * liftFactorByAngle * zone.Area;
            // применение силы в центре зоны
            var forceWorldSpace = transform.TransformVector(Vector3.up * liftForceMagnitude);
            Debug.DrawRay(transform.TransformPoint(zone.LocalPosition), forceWorldSpace * 0.1f, Color.red);
            if (float.IsNaN(forceWorldSpace.x))
            {
                return;
            }
            parentRigidbody.AddForceAtPosition(forceWorldSpace, transform.TransformPoint(zone.LocalPosition),
                    ForceMode.Force);


        }

        void OnDrawGizmosSelected()
        {
            // отображение формы крыла в редакторе
            Gizmos.matrix = transform.localToWorldMatrix;
            if (cachedCorners == null || cachedCorners.Length <= 0)
            {
                cachedCorners = CalculateCorners();
            }

            for (int i = 0; i < cachedCorners.Length - 1; ++i)
            {
                Gizmos.DrawLine(cachedCorners[i], cachedCorners[i + 1]);
            }

            Gizmos.DrawLine(cachedCorners[cachedCorners.Length - 1], cachedCorners[0]); // замыкающий сегмент

            if (zones == null)
            {
                zones = new List<AerodynamicZone>(EnumerateZones());
            }

            foreach (var aerodynamicZone in zones)
            {
                Vector3 up = transform.up * (aerodynamicZone.Area * 0.5f);
                Debug.DrawRay(transform.TransformPoint(aerodynamicZone.LocalPosition) - up, up * 2, Color.green);
            }
        }

        private Vector3[] CalculateCorners()
        {
            // формируем контур крыла
            return new[]
            {
                new Vector3(0f, 0f, -nearestEdgeLength * 0.5f), // левый нижний угол
                new Vector3(0, 0f, nearestEdgeLength * 0.5f), // левый верхний угол
                new Vector3(height, 0f, spanwiseOffset + farthestEdgeLength * 0.5f), // правый верхний угол
                new Vector3(height, 0f, spanwiseOffset - farthestEdgeLength * 0.5f) // правый нижний угол
            };
        }
    }
}