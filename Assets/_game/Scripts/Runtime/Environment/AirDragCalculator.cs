using System;
using System.Collections.Generic;
using Core.Utilities;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Runtime.Environment
{
    [ExecuteInEditMode]
    public class AirDragCalculator : MonoBehaviour
    {
        [Header("Objects")]
        public Camera cam;
        public ComputeShader blitShader;
        [Header("Setup")]
        public ShootLayer[] shootLayers;
        public int resolution = 512;
        public bool shoot = false;
        
        public List<Texture> result;

        private RenderTexture tex;
        private Transform current;
        private ComputeBuffer normalsBuffer;
        private Color[] pixels;
        private float radius;
        private Vector3 outCenter;
        [Button]

        public void CalculateSelfAirDrag()
        {
            CalculateAirDrag(transform);
        }

        private void Update()
        {
            if (shoot) CalculateSelfAirDrag();
        }

        public void CalculateAirDrag(Transform target)
        {
            current = target;
            var bounds = target.GetBounds();
            Vector3 center = bounds.center;
            radius = Vector3.Magnitude(bounds.center - bounds.min);
            
            SetupCam();
            SetupShader();

            outCenter = center;
            
            for (var i = 0; i < shootLayers.Length; i++)
            {
                for (var i1 = 0; i1 < shootLayers[i].shootDirections.Length; i1++)
                {
                    Vector3 d = shootLayers[i].shootDirections[i1].normalized;
                    TakeSnapshot(center - d * radius, d);
                }
            }
            
            
            tex.Release();
        }

        void SetupCam()
        {
            cam.enabled = false;
            cam.orthographicSize = radius;
            tex = new RenderTexture(resolution, resolution, 0) {enableRandomWrite = true};
            tex.Create();
            cam.targetTexture = tex;
        }

        private void SetupShader()
        {
            if (normalsBuffer != null && normalsBuffer.count != resolution * resolution)
            {
                normalsBuffer.Dispose();
                normalsBuffer = null;
            }

            if (normalsBuffer == null)
            {
                normalsBuffer = new ComputeBuffer(resolution * resolution, sizeof(float));
            }

            blitShader.SetTexture(0, "source", tex);
            blitShader.SetBuffer(0, "result", normalsBuffer);
            blitShader.SetInt("resx", resolution);
        }

        private void TakeSnapshot(Vector3 origin, Vector3 direction)
        {
            cam.transform.position = origin;
            Vector3 up;
            if (Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.998f)
            {
                up = -current.forward;
            }
            else
            {
                Vector3 cross = Vector3.Cross(Vector3.up, current.forward);
                up = Vector3.Cross(cross, Vector3.up);
                up.y = Mathf.Abs(up.y);
            }
            cam.transform.rotation = Quaternion.LookRotation(direction, up);
            
            cam.Render();

            ExtractRenderInfo();
        }

        private void ExtractRenderInfo()
        {
            int k = resolution / 8;
            blitShader.Dispatch(0, k, k, 1);
            float[] normals = new float[resolution * resolution];
            normalsBuffer.GetData(normals);

            Vector2 screenOffset = CalculateOffset(normals, out float sum);

            Ray ray = cam.ScreenPointToRay(new Vector3(screenOffset.x * resolution, screenOffset.y * resolution, 1));
            
            Vector3 addOffset = Vector3.ProjectOnPlane(ray.origin - outCenter, ray.direction);
            outCenter += addOffset;
            
            Debug.DrawRay(ray.origin, ray.direction * radius, Color.red, shoot ? 0.02f : 15);
            Debug.DrawRay(ray.origin, -ray.direction * sum, Color.blue, shoot ? 0.02f : 15);
        }

        private Vector2 CalculateOffset(float[] normals, out float sum)
        {
            int res = resolution;
            //int halfRes = res / 2;
            Vector2 offset = Vector2.zero;
            sum = 0;
            float nrm = 0;
            float space = 0;
            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    nrm = normals[y * res + x];
                    sum += nrm;
                    offset += new Vector2(x, y) * nrm;
                    if (nrm != 0) space++;
                }
            }

            float inv = 1f / (sum * res);
            offset *= inv;
            if (space != 0)
            {
                sum /= space;
            }
            return offset;
        }
    }

    [System.Serializable]
    public class ShootLayer
    {
        public Vector3[] shootDirections;
    }
    
}
