using System.Collections.Generic;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Environment.AirDrag
{
    public class AirDragCalculator
    {
        private Vector3[] shootDirections = new Vector3[]
        {
            Vector3.forward,
            Vector3.right,
            Vector3.back,
            Vector3.left,
            Vector3.up,
            Vector3.down
        };
        
        private Transform current;
        private float radius;
        private Vector3 center;
        private AirDragBehaviour data;

        public ShootLayerResult CalculateAirDrag(Transform target, AirDragBehaviour data)
        {
            this.data = data;
            current = target;
            Bounds bounds = target.GetBounds();
            center = bounds.center + target.position;
            radius = Vector3.Magnitude(bounds.center - bounds.min);

            data.Cam.orthographicSize = radius;
            data.Cam.farClipPlane = radius * 2;

            ShootLayerResult resultLayer = new ShootLayerResult();
            foreach (Vector3 direction in shootDirections)
            {
                var d = current.TransformDirection(direction);
                TakeSnapshot(center - d * radius, d, resultLayer);
            }

            resultLayer.Initialize();

            return resultLayer;
        }

        private void TakeSnapshot(Vector3 origin, Vector3 direction, ShootLayerResult result)
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
    
[ShowInInspector]
    public class AirDragProfile
    {
        [ShowInInspector] private ShootLayerResult layerResult;
        private AirDragBehaviour behaviour;

        public AirDragProfile(ShootLayerResult layerResult, AirDragBehaviour behaviour)
        {
            this.layerResult = layerResult;
            this.behaviour = behaviour;
        }

        public (Vector3 drag, Vector3 normal, Vector3 position) CalculateForce(Vector3 localWindForce)
        {
            float windSpeed = localWindForce.magnitude;
            if (windSpeed == 0) return (Vector3.zero, Vector3.zero, Vector3.zero);
            
            Vector3 windDirection = -localWindForce / windSpeed;
            
            (Vector3 normal, Vector3 position) = CalculateDrag(windDirection);

            float dot = Vector3.Dot(localWindForce, normal.normalized);
            Vector3 drag = -windDirection * (normal.magnitude * behaviour.turbulenceImpact * windSpeed);
            Vector3 normalForce = normal * (dot * behaviour.normalForceImpact);
            return ((drag + normalForce) * windSpeed, normal, position);
        }
        
        private (Vector3 normal, Vector3 position) CalculateDrag(Vector3 direction)
        {
            float azimuth = Mathf.Atan2(direction.x, direction.z);
            if (azimuth < 0) azimuth += Mathf.PI * 2;
            float sign = Mathf.Sign(direction.y);
            float sqrt = Mathf.Sqrt(Mathf.Abs(direction.y));
            float altitude = sqrt * sign;
            
            (Vector3 normal, Vector3 position) = layerResult.CalculateForce(azimuth, altitude);
            
            return (normal, position);
        }
    }
    [ShowInInspector]
    public class ShootLayerResult
    {
        [ShowInInspector]
        private List<DirectionSnapshot> snapShots = new List<DirectionSnapshot>();

        private readonly float[] azimuthEdges =
        {
            0f,
            Mathf.PI / 2f,
            Mathf.PI,
            Mathf.PI / 2f * 3f,
            Mathf.PI * 2,
        };
        
        public void WriteResult(float space, Vector3 normal, Vector3 centerOffset)
        {
            snapShots.Add(new DirectionSnapshot(space, normal, centerOffset));
        }

        public void Initialize()
        {
            float centerX = (snapShots[4].centerOffset.x + snapShots[5].centerOffset.x) * 0.5f;
            snapShots[1].centerOffset.x = centerX;
            snapShots[3].centerOffset.x = centerX;
            
            float centerY = (snapShots[0].centerOffset.y + snapShots[2].centerOffset.y) * 0.5f;
            snapShots[4].centerOffset.y = centerY;
            snapShots[5].centerOffset.y = centerY;
            
            float centerZ = (snapShots[4].centerOffset.z + snapShots[5].centerOffset.z) * 0.5f;
            snapShots[0].centerOffset.z = centerZ;
            snapShots[2].centerOffset.z = centerZ;
        }

        public (Vector3 normal, Vector3 position) CalculateForce(float azimuth, float altitude)
        {
            int i;
            for (i = 0; i < azimuthEdges.Length - 1; i++)
            {
                if (azimuth > azimuthEdges[i] && azimuth <= azimuthEdges[i + 1]) break;
            }
            
            float lastAzimuth = azimuthEdges[i];
            float nextAzimuth = azimuthEdges[(i + 1) % 5];
                
            DirectionSnapshot last = snapShots[i];
            DirectionSnapshot next = snapShots[(i + 1) % 4];

            float lerp = (azimuth - lastAzimuth) / (nextAzimuth - lastAzimuth);

            Vector3 normal = Vector3.Lerp(last.bakedSpace * last.bakedNormal, next.bakedSpace * next.bakedNormal, lerp);
            Vector3 position = Vector3.Lerp(last.centerOffset, next.centerOffset, lerp);

            DirectionSnapshot yComponent;
            if (altitude > 0)
            {
                yComponent = snapShots[4];
            }
            else
            {
                altitude = -altitude;
                yComponent = snapShots[5];
            }
            
            normal = Vector3.Lerp(normal, yComponent.bakedNormal * yComponent.bakedSpace, altitude);
            position = Vector3.Lerp(position, yComponent.centerOffset, altitude);
            
            return (normal, position);
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
