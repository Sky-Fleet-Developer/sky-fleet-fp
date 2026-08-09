using System;
using System.Collections.Generic;
using Core.Misc;
using Core.Structure;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Zenject;
using ITickable = Core.Misc.ITickable;

namespace Runtime.Environment.AirDrag
{
    public class AirDragSystem : MonoBehaviour, ITickable
    {
        private static readonly int SourceProperty = Shader.PropertyToID("source");
        private static readonly int ResultProperty = Shader.PropertyToID("result");
        private static readonly int XResolutionProperty = Shader.PropertyToID("resx");

        [SerializeField] private AirDragSettings settings;
        [NonSerialized] private Camera _cam;
        [NonSerialized] private ComputeBuffer _resultBuffer;
        [Inject] private TickService _tickService;
        [Inject] private StructureUpdateSystem _structureUpdateSystem;

        private RenderTexture _texture;
        private Material[] _materialArray;

        [ShowInInspector, ReadOnly] private readonly Dictionary<IDynamicStructure, AirDragProfile> _profiles = new(10);
        private readonly AirDragCalculator _calculator = new AirDragCalculator();
        public int TickRate => 1;

        static AirDragSystem()
        {
            TickService.SetUpdate(typeof(AirDragSystem), true);
            TickService.SetOrderAfter(typeof(AirDragSystem), typeof(StructureUpdateSystem));
        }

        private void Start()
        {
            if (!settings || !settings.enableAirDrag)
            {
                enabled = false;
                return;
            }
            _materialArray = new Material[10];
            for (var i = 0; i < _materialArray.Length; i++)
            {
                _materialArray[i] = settings.material;
            }
            _structureUpdateSystem.OnInitialize.Subscribe(InitializeEntities);
            _structureUpdateSystem.OnStructureAdd += CalculateDragFor;
            _structureUpdateSystem.OnStructureRemoved += RemoveStructure;
        }

        private async void OnEnable()
        {
            while(_tickService == null)
            {
                await UniTask.Yield();
            }
            _tickService.Add(this);
        }

        private void OnDisable()
        {
            _tickService.Remove(this);
        }

        private void OnDestroy()
        {
            _structureUpdateSystem.OnStructureAdd -= CalculateDragFor;
            _structureUpdateSystem.OnStructureRemoved -= RemoveStructure;
        }

        private void InitializeEntities()
        {
            foreach (IStructure structure in _structureUpdateSystem.Structures())
            {
                if (structure is IDynamicStructure dynamicStructure)
                {
                    CalculateDragFor(dynamicStructure);
                }
            }
        }

        public void Tick()
        {
            foreach (KeyValuePair<IDynamicStructure, AirDragProfile> structure in _profiles)
            {
                ApplyWind(structure.Key, structure.Value);
            }
        }
        
        private void ApplyWind(IDynamicStructure structure, AirDragProfile profile)
        {
            Vector3 windVelocity = -structure.Velocity;
            (Vector3 drag, Vector3 normal, Vector3 position) =
                profile.CalculateForce(structure.transform.InverseTransformDirection(windVelocity));

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

            if (!_cam) CreateCamera();
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
                DestroyImmediate(_cam.gameObject);
                _cam = null;
            }
        }

        private void Process(IDynamicStructure structure)
        {
            Dictionary<Renderer, (Material[] materials, int layer)> oldMaterials =
                new Dictionary<Renderer, (Material[] materials, int layer)>();

            foreach (MeshRenderer renderer in structure.transform.GetComponentsInChildren<MeshRenderer>())
            {
                oldMaterials.Add(renderer, (renderer.sharedMaterials, renderer.gameObject.layer));
                renderer.gameObject.layer = settings.layer;
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    renderer.sharedMaterials = _materialArray;
                }
            }


            AirDragProfile result = new AirDragProfile(_calculator.CalculateAirDrag(structure.transform, settings, _resultBuffer, _cam), settings);
            if (!_profiles.ContainsKey(structure))
            {
                _profiles.Add(structure, result);
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
            _profiles.Remove(dynamicStructure);
        }

        private void CreateCamera()
        {
            _cam = new GameObject("AirDragCamera").AddComponent<Camera>();
            var hd = _cam.gameObject.AddComponent<HDAdditionalCameraData>();

            hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            hd.dithering = false;
            hd.volumeLayerMask = 0;
            hd.backgroundColorHDR = Color.clear;
            hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;

            if (_texture == null)
            {
                _texture = new RenderTexture(settings.resolution, settings.resolution, 0) { enableRandomWrite = true };
                _texture.Create();
            }

            _cam.enabled = false;
            _cam.orthographic = true;
            _cam.cullingMask = settings.mask;
            _cam.targetTexture = _texture;
            _cam.nearClipPlane = 0;
        }

        private void RecreateBuffer()
        {
            _resultBuffer ??= new ComputeBuffer(AirDragSettings.ResultBufferSize, sizeof(float));
            _resultBuffer.SetData(new float[AirDragSettings.ResultBufferSize]);
            settings.pixelsToNormalsShader.SetBuffer(0, ResultProperty, _resultBuffer);
            settings.pixelsToNormalsShader.SetTexture(0, SourceProperty, _texture);
            settings.pixelsToNormalsShader.SetInt(XResolutionProperty, settings.resolution);
        }
    }
}