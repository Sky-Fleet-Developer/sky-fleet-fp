using System;
using System.Collections.Generic;
using Core.Structure;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static Core.Structure.CycleService;

namespace Runtime.Environment.AirDrag
{
    [CreateAssetMenu(menuName = "Data/AirDrag")]
    public class AirDragBehaviour : ScriptableObject
    {
        public Material material;
        public ComputeShader pixelsToNormalsShader;
        public int resolution = 256;
        [Space(15)] public float turbulenceImpact = 1f;
        [Space(15)] public float normalForceImpact = 1f;
        [Space(15)]
        [SerializeField] private LayerMask mask;
        [SerializeField] private int layer;

        [NonSerialized] public Camera Cam;
        [NonSerialized] public ComputeBuffer Buffer;

        private RenderTexture texture;
        private Material[] materialArray;

        [ShowInInspector, ReadOnly] private readonly Dictionary<IDynamicStructure, AirDragProfile> profiles = new (10);
        private readonly AirDragCalculator calculator = new AirDragCalculator();
        
        private void OnEnable()
        {
            materialArray = new Material[10];
            for (var i = 0; i < materialArray.Length; i++)
            {
                materialArray[i] = material;
            }
            foreach (IStructure structure in Structures)
            {
                if (structure is IDynamicStructure dynamicStructure)
                {
                    CalculateDragFor(dynamicStructure);
                }
            }
            OnStructureInitialized += CalculateDragFor;
            OnStructureUnregistered += RemoveStructure;
            OnEndPhysicsTick += PhysicsTick;
        }

        private void PhysicsTick()
        {
            foreach (KeyValuePair<IDynamicStructure, AirDragProfile> structure in profiles)
            {
                ApplyWind(structure.Key, structure.Value);
            }
        }

        private void ApplyWind(IDynamicStructure structure, AirDragProfile profile)
        {
            Vector3 windVelocity = -structure.Velocity;
            (Vector3 drag, Vector3 normal, Vector3 position) = profile.CalculateForce(structure.transform.InverseTransformDirection(windVelocity));

            drag = structure.transform.TransformDirection(drag);
            position = structure.transform.TransformPoint(position);
            normal = structure.transform.TransformDirection(normal);
            
            Debug.DrawRay(position, normal.normalized * 2, Color.blue);
            Debug.DrawRay(position, drag * 0.001f, Color.red);
            
            structure.AddForce(drag, position);
        }

        private void CalculateDragFor(IStructure structure)
        {
            if (structure is not IDynamicStructure dynamicStructure) return;
            
            if (!Cam) CreateCamera();
            RecreateBuffer();
            try
            {
                Process(dynamicStructure);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (!Application.isPlaying)
            {
                DestroyImmediate(Cam.gameObject);
                Cam = null;
            }
        }

        private void Process(IDynamicStructure structure)
        {
            Dictionary<Renderer, (Material[] materials, int layer)> oldMaterials = new Dictionary<Renderer, (Material[] materials, int layer)>();

            foreach (MeshRenderer renderer in structure.transform.GetComponentsInChildren<MeshRenderer>())
            {
                oldMaterials.Add(renderer, (renderer.sharedMaterials, renderer.gameObject.layer));
                renderer.gameObject.layer = layer;
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    renderer.sharedMaterials = materialArray;
                }
            }

            
            AirDragProfile result = new AirDragProfile(calculator.CalculateAirDrag(structure.transform, this), this);
            if (!profiles.ContainsKey(structure))
            {
                profiles.Add(structure, result);
            }

            foreach (KeyValuePair<Renderer, (Material[] materials, int layer)> renderer in oldMaterials)
            {
                renderer.Key.gameObject.layer = renderer.Value.layer;
                for (int i = 0; i < renderer.Value.materials.Length; i++)
                {
                    renderer.Key.sharedMaterials = renderer.Value.materials;
                }
            }
        }

        private void RemoveStructure(IStructure structure)
        {
            if (!(structure is IDynamicStructure dynamicStructure)) return;
            profiles.Remove(dynamicStructure);
        }

        private void CreateCamera()
        {
            Cam = new GameObject("AirDragCamera").AddComponent<Camera>();
            var hd = Cam.gameObject.AddComponent<HDAdditionalCameraData>();

            hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            hd.dithering = false;
            hd.volumeLayerMask = 0;
            hd.backgroundColorHDR = Color.clear;
            hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            
            if (texture == null)
            {
                texture = new RenderTexture(resolution, resolution, 0) {enableRandomWrite = true};
                texture.Create();
            }

            Cam.enabled = false;
            Cam.orthographic = true;
            Cam.cullingMask = mask;
            Cam.targetTexture = texture;
            Cam.nearClipPlane = 0;
        }
        
        private void RecreateBuffer()
        {
            if (Buffer != null && Buffer.count != resolution * resolution)
            {
                Buffer.Dispose();
                Buffer = null;
            }

            if (Buffer == null)
            {
                Buffer = new ComputeBuffer(resolution * resolution, sizeof(float) * 3);
                pixelsToNormalsShader.SetBuffer(0, "result", Buffer);
                pixelsToNormalsShader.SetTexture(0, "source", texture);
                pixelsToNormalsShader.SetInt("resx", resolution);
            }
        }
    }
}
