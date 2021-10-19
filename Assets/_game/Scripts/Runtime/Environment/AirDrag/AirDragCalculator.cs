using System.Collections;
using System.Collections.Generic;
using Core.Utilities;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Runtime.Environment.AirDrag
{
    public class AirDragCalculator
    {
        private Transform current;
        private float radius;
        private Vector3 center;
        private AirDragBehaviour data;
        
        public List<ShootLayerResult> CalculateAirDrag(Transform target, AirDragBehaviour data)
        {
            this.data = data;
            current = target;
            Bounds bounds = target.GetBounds();
            center = bounds.center;
            radius = Vector3.Magnitude(bounds.center - bounds.min);

            data.Cam.orthographicSize = radius;
            data.Cam.farClipPlane = radius * 2;
            
            List<ShootLayerResult> result = new List<ShootLayerResult>(data.layers.Count);
            
            for (int i = 0; i < data.layers.Count; i++)
            {
                ShootLayerSettings layer =  data.layers[i];
                ShootLayerResult resultLayer = new ShootLayerResult(layer.altitude);
                layer.Reset();
                foreach (Vector3 direction in layer)
                {
                    var d = current.TransformDirection(direction);
                    TakeSnapshot(center - d * radius, d, layer, resultLayer);
                }
                result.Add(resultLayer);
            }
            
            return result;
        }

        private void TakeSnapshot(Vector3 origin, Vector3 direction, ShootLayerSettings settings, ShootLayerResult result)
        {
            data.Cam.transform.position = origin;
            Vector3 up;
            if (Mathf.Abs(Vector3.Dot(direction, current.up)) > 0.998f)
            {
                up = -current.forward;
            }
            else
            {
                Vector3 cross = Vector3.Cross(current.up, current.forward);
                up = Vector3.Cross(cross, current.up);
                up.y = Mathf.Abs(up.y);
            }
            data.Cam.transform.rotation = Quaternion.LookRotation(direction, up);
            
            data.Cam.Render();

            ExtractRenderInfo(result);
        }

        private void ExtractRenderInfo(ShootLayerResult result)
        {
            int k = data.resolution / 8;
            data.pixelsToNormalsShader.Dispatch(0, k, k, 1);
            Vector3[] normals = new Vector3[data.resolution * data.resolution];
            data.Buffer.GetData(normals);

            Vector2 screenOffset = CalculateOffset(normals, out Vector3 normal, out int space);

            Ray ray = data.Cam.ScreenPointToRay(new Vector3(screenOffset.x * data.resolution, screenOffset.y * data.resolution, 1));
            Debug.DrawRay(ray.origin, normal, Color.blue, 15);
            normal = current.InverseTransformDirection(normal);
            
            Vector3 normalOffset = Vector3.ProjectOnPlane(ray.origin - current.position, ray.direction);
            Vector3 dotOffset = Vector3.Project(center - current.position, ray.direction);

            Vector3 localOffset = current.InverseTransformDirection(normalOffset + dotOffset);

            float cameraSpace = radius * radius * 4;
            float pixelSpace = cameraSpace / (data.resolution * data.resolution);
            result.WriteResult(space * pixelSpace, normal, localOffset);
            
            Debug.DrawRay(ray.origin, ray.direction * radius, Color.red, 15);
            Debug.DrawRay(ray.origin, -ray.direction * normal.magnitude, Color.yellow, 15);
            Debug.DrawRay(data.Cam.transform.position, data.Cam.transform.forward, Color.black, 5);
        }

        private Vector2 CalculateOffset(Vector3[] normals, out Vector3 normal, out int space)
        {
            int res = data.resolution;
            Vector2 offset = Vector2.zero;
            normal = Vector3.zero;
            space = 0;
            
            Vector3 nrm = Vector3.zero;
            Vector3 fwd = -data.Cam.transform.forward;
            float dotSumm = 0;
            float dot = 0;
            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    nrm = normals[y * res + x];
                    normal += nrm;
                    dot = Vector3.Dot(nrm, fwd);
                    dotSumm += dot;
                    offset += new Vector2(x, y) * dot;
                    if (nrm.sqrMagnitude != 0) space++;
                }
            }

            float inv = 1f / Mathf.Abs(dotSumm * res);
            offset *= inv;
            if (space != 0)
            {
                normal /= space;
            }
            return offset;
        }
    }

    [System.Serializable]
    public class ShootLayerSettings : IEnumerator<Vector3>, IEnumerable
    {
        [SerializeField] private int roundShoots = 1;
        [SerializeField, Range(-1, 1)] public float altitude = 0;
        
        private int pointer = 0;

        public bool MoveNext()
        {
            return ++pointer < roundShoots;
        }

        public void Reset()
        {
            pointer = -1;
        }

        public Vector3 Current
        {
            get
            {
                if (Mathf.Abs(Mathf.Abs(altitude) - 1) < 0.001f) return Vector3.up * altitude;
                float azimuth = (pointer * Mathf.PI * 2f) / roundShoots;
                float mul = Mathf.Sqrt(1 - altitude * altitude);
                Vector3 res = new Vector3(Mathf.Sin(azimuth) * mul, altitude, Mathf.Cos(azimuth) * mul);
                return res;
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            pointer = -1;
        }

        public IEnumerator GetEnumerator()
        {
            return this;
        }
    }
[ShowInInspector]
    public class AirDragProfile
    {
        [ShowInInspector] private List<ShootLayerResult> layers;
        private AirDragBehaviour behaviour;

        public AirDragProfile(List<ShootLayerResult> layers, AirDragBehaviour behaviour)
        {
            this.layers = layers;
            this.behaviour = behaviour;
            layers.Sort((a, b) => a.altitude.CompareTo(b.altitude));
        }

        public (Vector3 drag, Vector3 position) CalculateForce(Vector3 localWindForce)
        {
            float windSpeed = localWindForce.magnitude;
            if (windSpeed == 0) return (Vector3.zero, Vector3.zero);
            
            Vector3 windDirection = -localWindForce / windSpeed;
            
            (Vector3 normal, Vector3 position) = CalculateDrag(windDirection);

            float dot = Vector3.Dot(localWindForce, normal.normalized);
            Vector3 drag = -windDirection * (normal.magnitude * behaviour.turbulenceImpact * windSpeed);
            Vector3 normalForce = normal * (dot * behaviour.normalForceImpact);
            return ((drag + normalForce) * windSpeed, position);
        }
        
        private (Vector3 normal, Vector3 position) CalculateDrag(Vector3 direction)
        {
            float altitude = Mathf.Clamp(direction.y, layers[0].altitude, layers[layers.Count - 1].altitude);

            float azimuth = Mathf.Atan2(direction.x, direction.z);
            if (azimuth < 0) azimuth += Mathf.PI * 2;

            int alt = 0;
            
            for (int i = 0; i < layers.Count - 1; i++)
            {
                if (altitude <= layers[i + 1].altitude)
                {
                    alt = i;
                    break;
                }
            }

            ShootLayerResult min = layers[alt];
            ShootLayerResult max = layers[alt + 1];

            if (altitude == min.altitude) return min.CalculateForce(azimuth);
            if (altitude == max.altitude) return max.CalculateForce(azimuth);

            (Vector3 normal, Vector3 position) minForce = min.CalculateForce(azimuth);
            (Vector3 normal, Vector3 position) maxForce = max.CalculateForce(azimuth);

            float lerp = (altitude - min.altitude) / (max.altitude - min.altitude);
            
            Vector3 normal = Vector3.Lerp(minForce.normal, maxForce.normal, lerp);
            Vector3 position = Vector3.Lerp(minForce.position, maxForce.position, lerp);
            
            return (normal, position);
        }
    }
    [ShowInInspector]
    public class ShootLayerResult
    {
        [ShowInInspector]
        private List<DirectionSnapshot> snapShots = new List<DirectionSnapshot>();

        public float altitude;

        public ShootLayerResult(float altitude)
        {
            this.altitude = altitude;
        }

        public void WriteResult(float space, Vector3 normal, Vector3 centerOffset)
        {
            snapShots.Add(new DirectionSnapshot(space, normal, centerOffset));
        }

        public (Vector3 normal, Vector3 position) CalculateForce(float azimuth)
        {
            for (int i = 0; i < snapShots.Count; i++)
            {
                float lastAzimuth = GetAzimuth(i);
                float nextAzimuth = GetAzimuth(i + 1);

                if (!(azimuth > lastAzimuth) || !(azimuth <= nextAzimuth)) continue;
                
                DirectionSnapshot last = snapShots[i];
                DirectionSnapshot next = snapShots[(i + 1) % snapShots.Count];

                float lerp = (azimuth - lastAzimuth) / (nextAzimuth - lastAzimuth);

                return (Vector3.Lerp(last.bakedSpace * last.bakedNormal, next.bakedSpace * next.bakedNormal, lerp),
                    Vector3.Lerp(last.centerOffset, next.centerOffset, lerp));
            }

            return (Vector3.zero, Vector3.zero);
        }

        private float GetAzimuth(int idx)
        {
            return idx * Mathf.PI * 2f / snapShots.Count;
        }
    }
    [ShowInInspector]
    public class DirectionSnapshot
    {
        [ShowInInspector] public float bakedSpace;
        [ShowInInspector] public Vector3 bakedNormal;
        [ShowInInspector] public Vector3 centerOffset;

        public DirectionSnapshot(float bakedSpace, Vector3 bakedNormal, Vector3 centerOffset)
        {
            this.bakedSpace = bakedSpace;
            this.bakedNormal = bakedNormal;
            this.centerOffset = centerOffset;
        }
    }
    
}
