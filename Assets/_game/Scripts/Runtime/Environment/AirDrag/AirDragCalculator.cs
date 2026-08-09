using System.Collections.Generic;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Environment.AirDrag
{
    public class AirDragCalculator
    {
        private static readonly int FwdProperty = Shader.PropertyToID("fwd");

        private Vector3[] _shootDirections = new Vector3[]
        {
            Vector3.forward,
            Vector3.right,
            Vector3.back,
            Vector3.left,
            Vector3.up,
            Vector3.down,
        };
        
        private Transform _current;
        private float _radius;
        private Vector3 _center;
        private AirDragSettings _data;

        public ShootLayerResult CalculateAirDrag(Transform target, AirDragSettings data, ComputeBuffer resultBuffer, Camera camera)
        {
            this._data = data;
            _current = target;
           

            ShootLayerResult resultLayer = new ShootLayerResult();
            foreach (Vector3 direction in _shootDirections)
            {
                Bounds bounds = target.GetBounds();
                bounds.center += target.position;
                DrawBounds(bounds);
                _center = bounds.center;
                _radius = bounds.extents.magnitude;

                camera.orthographicSize = _radius;
                camera.farClipPlane = _radius * 2;
                var d = _current.TransformDirection(direction);
                TakeSnapshot(_center - d * _radius, d, resultLayer, resultBuffer, camera);
            }

            resultLayer.Initialize();

            return resultLayer;
        }

        private static void DrawBounds(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            // Draw the 8 corners of the bounds
            Vector3 v3FrontTopLeft    = center + new Vector3(-size.x,  size.y, -size.z) * 0.5f;
            Vector3 v3FrontTopRight   = center + new Vector3( size.x,  size.y, -size.z) * 0.5f;
            Vector3 v3FrontBottomLeft  = center + new Vector3(-size.x, -size.y, -size.z) * 0.5f;
            Vector3 v3FrontBottomRight = center + new Vector3( size.x, -size.y, -size.z) * 0.5f;
            Vector3 v3BackTopLeft     = center + new Vector3(-size.x,  size.y,  size.z) * 0.5f;
            Vector3 v3BackTopRight    = center + new Vector3( size.x,  size.y,  size.z) * 0.5f;
            Vector3 v3BackBottomLeft   = center + new Vector3(-size.x, -size.y,  size.z) * 0.5f;
            Vector3 v3BackBottomRight  = center + new Vector3( size.x, -size.y,  size.z) * 0.5f;

            // Front face
            Debug.DrawLine(v3FrontTopLeft, v3FrontTopRight, Color.red);
            Debug.DrawLine(v3FrontTopRight, v3FrontBottomRight, Color.red);
            Debug.DrawLine(v3FrontBottomRight, v3FrontBottomLeft, Color.red);
            Debug.DrawLine(v3FrontBottomLeft, v3FrontTopLeft, Color.red);

            // Back face
            Debug.DrawLine(v3BackTopLeft, v3BackTopRight, Color.red);
            Debug.DrawLine(v3BackTopRight, v3BackBottomRight, Color.red);
            Debug.DrawLine(v3BackBottomRight, v3BackBottomLeft, Color.red);
            Debug.DrawLine(v3BackBottomLeft, v3BackTopLeft, Color.red);

            // Connect front and back faces
            Debug.DrawLine(v3FrontTopLeft, v3BackTopLeft, Color.red);
            Debug.DrawLine(v3FrontTopRight, v3BackTopRight, Color.red);
            Debug.DrawLine(v3FrontBottomRight, v3BackBottomRight, Color.red);
            Debug.DrawLine(v3FrontBottomLeft, v3BackBottomLeft, Color.red);
        }

        private void TakeSnapshot(Vector3 origin, Vector3 direction, ShootLayerResult result, ComputeBuffer resultBuffer, Camera camera)
        {
            camera.transform.position = origin;
            Vector3 up;
            if (Mathf.Abs(Vector3.Dot(direction, _current.up)) > 0.998f)
            {
                up = -_current.forward;
            }
            else
            {
                Vector3 cross = Vector3.Cross(_current.up, _current.forward);
                up = Vector3.Cross(cross, _current.up);
                up.y = Mathf.Abs(up.y);
            }
            camera.transform.rotation = Quaternion.LookRotation(direction, up);
            
            camera.Render();

            ExtractRenderInfo(result, resultBuffer, camera);
        }
        
        private void ExtractRenderInfo(ShootLayerResult result, ComputeBuffer resultBuffer, Camera camera)
        {
            int k = _data.resolution / 8;
            _data.pixelsToNormalsShader.SetVector(FwdProperty, -camera.transform.forward);
            _data.pixelsToNormalsShader.Dispatch(0, k, k, 1);
            int[] shaderResult = new int[AirDragSettings.ResultBufferSize];
            resultBuffer.GetData(shaderResult);

            Vector2 screenOffset = ExtractResult(shaderResult, out Vector3 normal, out int space);

            Ray ray = camera.ScreenPointToRay(new Vector3(screenOffset.x * _data.resolution, screenOffset.y * _data.resolution, 1));
            //Debug.Log($"offset = {screenOffset}, origin = {ray.origin}");
            Debug.DrawRay(ray.origin, normal, Color.blue, 15);
            Debug.DrawRay(camera.ScreenPointToRay(new Vector3(0, 0, 1)).origin, ray.direction, Color.cyan, 15);
            Debug.DrawRay(camera.ScreenPointToRay(new Vector3(0, _data.resolution, 1)).origin, ray.direction, Color.cyan, 15);
            Debug.DrawRay(camera.ScreenPointToRay(new Vector3(_data.resolution, 0, 1)).origin, ray.direction, Color.cyan, 15);
            Debug.DrawRay(camera.ScreenPointToRay(new Vector3(_data.resolution, _data.resolution, 1)).origin, ray.direction, Color.cyan, 15);
            normal = _current.InverseTransformDirection(normal);
            
            Vector3 normalOffset = Vector3.ProjectOnPlane(ray.origin - _current.position, ray.direction);
            Vector3 dotOffset = Vector3.Project(_center - _current.position, ray.direction);

            Vector3 localOffset = _current.InverseTransformDirection(normalOffset + dotOffset);
            Vector3 wo = _current.InverseTransformPoint(localOffset);
            Debug.DrawRay(wo, normal, Color.blue, 15);

            float cameraSpace = _radius * _radius * 4;
            float pixelSpace = cameraSpace / (_data.resolution * _data.resolution);
            result.WriteResult(space * pixelSpace, normal, localOffset);
            
            Debug.DrawRay(ray.origin, ray.direction * _radius, Color.red, 15);
            Debug.DrawRay(ray.origin, -ray.direction * normal.magnitude, Color.yellow, 15);
            Debug.DrawRay(camera.transform.position, camera.transform.forward, Color.black, 5);
        }

        private Vector2 ExtractResult(int[] shaderResult, out Vector3 normal, out int filledPixelsCount)
        {
            int res = _data.resolution;
            normal = new Vector3(shaderResult[0] / 255f, shaderResult[1] / 255f, shaderResult[2] / 255f);
            filledPixelsCount = shaderResult[3];
            float dotSum = shaderResult[4] / 255f;
            Vector2 offset = new Vector2(shaderResult[5], shaderResult[6]);

            offset *= 1f / (dotSum * res);
            if (filledPixelsCount != 0)
            {
                normal /= filledPixelsCount;
            }
            //Debug.Log($"Extacted: normal = {normal}, offset = {offset}, filledPixelsCount = {filledPixelsCount}, dotSum = {dotSum}");
            return offset;
        }
    }
    
    [ShowInInspector]
    public class AirDragProfile
    {
        [ShowInInspector] private ShootLayerResult _layerResult;
        private AirDragSettings _settings;

        public AirDragProfile(ShootLayerResult layerResult, AirDragSettings settings)
        {
            this._layerResult = layerResult;
            _settings = settings;
        }

        public (Vector3 drag, Vector3 normal, Vector3 position) CalculateForce(Vector3 localWindForce)
        {
            float windSpeed = localWindForce.magnitude;
            if (windSpeed == 0) return (Vector3.zero, Vector3.zero, Vector3.zero);
            
            Vector3 windDirection = -localWindForce / windSpeed;
            
            (Vector3 normal, Vector3 position) = CalculateDrag(windDirection);

            float dot = Vector3.Dot(localWindForce, normal.normalized);
            Vector3 drag = -windDirection * (normal.magnitude * _settings.turbulenceImpact * windSpeed);
            Vector3 normalForce = normal * (dot * _settings.normalForceImpact);
            return ((drag + normalForce) * windSpeed, normal, position);
        }
        
        private (Vector3 normal, Vector3 position) CalculateDrag(Vector3 direction)
        {
            float azimuth = Mathf.Atan2(direction.x, direction.z);
            if (azimuth < 0) azimuth += Mathf.PI * 2;
            float sign = Mathf.Sign(direction.y);
            float sqrt = Mathf.Sqrt(Mathf.Abs(direction.y));
            float altitude = sqrt * sign;
            
            (Vector3 normal, Vector3 position) = _layerResult.CalculateForce(azimuth, altitude);
            
            return (normal, position);
        }
    }
    [ShowInInspector]
    public class ShootLayerResult
    {
        [ShowInInspector]
        private List<DirectionSnapshot> _snapShots = new List<DirectionSnapshot>();

        private readonly float[] _azimuthEdges =
        {
            0f,
            Mathf.PI / 2f,
            Mathf.PI,
            Mathf.PI / 2f * 3f,
            Mathf.PI * 2,
        };
        
        public void WriteResult(float space, Vector3 normal, Vector3 centerOffset)
        {
            _snapShots.Add(new DirectionSnapshot(space, normal, centerOffset));
        }

        public void Initialize()
        {
            float centerX = (_snapShots[4].CenterOffset.x + _snapShots[5].CenterOffset.x) * 0.5f;
            _snapShots[1].CenterOffset.x = centerX;
            _snapShots[3].CenterOffset.x = centerX;
            
            float centerY = (_snapShots[0].CenterOffset.y + _snapShots[2].CenterOffset.y) * 0.5f;
            _snapShots[4].CenterOffset.y = centerY;
            _snapShots[5].CenterOffset.y = centerY;
            
            float centerZ = (_snapShots[4].CenterOffset.z + _snapShots[5].CenterOffset.z) * 0.5f;
            _snapShots[0].CenterOffset.z = centerZ;
            _snapShots[2].CenterOffset.z = centerZ;
        }

        public (Vector3 normal, Vector3 position) CalculateForce(float azimuth, float altitude)
        {
            int i;
            for (i = 0; i < _azimuthEdges.Length - 1; i++)
            {
                if (azimuth > _azimuthEdges[i] && azimuth <= _azimuthEdges[i + 1]) break;
            }
            
            float lastAzimuth = _azimuthEdges[i];
            float nextAzimuth = _azimuthEdges[(i + 1) % 5];
                
            DirectionSnapshot last = _snapShots[i];
            DirectionSnapshot next = _snapShots[(i + 1) % 4];

            float lerp = (azimuth - lastAzimuth) / (nextAzimuth - lastAzimuth);

            Vector3 normal = Vector3.Lerp(last.BakedSpace * last.BakedNormal, next.BakedSpace * next.BakedNormal, lerp);
            Vector3 position = Vector3.Lerp(last.CenterOffset, next.CenterOffset, lerp);

            DirectionSnapshot yComponent;
            if (altitude > 0)
            {
                yComponent = _snapShots[4];
            }
            else
            {
                altitude = -altitude;
                yComponent = _snapShots[5];
            }
            
            normal = Vector3.Lerp(normal, yComponent.BakedNormal * yComponent.BakedSpace, altitude);
            position = Vector3.Lerp(position, yComponent.CenterOffset, altitude);
            
            return (normal, position);
        }
    }
    [ShowInInspector]
    public class DirectionSnapshot
    {
        [ShowInInspector] public float BakedSpace;
        [ShowInInspector] public Vector3 BakedNormal;
        [ShowInInspector] public Vector3 CenterOffset;

        public DirectionSnapshot(float bakedSpace, Vector3 bakedNormal, Vector3 centerOffset)
        {
            this.BakedSpace = bakedSpace;
            this.BakedNormal = bakedNormal;
            this.CenterOffset = centerOffset;
        }
    }
    
}
