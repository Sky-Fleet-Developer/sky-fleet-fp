﻿#undef DEBUG_ENABLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Structure;
using Core.Utilities;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using Voxels.Procedural;
using Zenject;
using Random = UnityEngine.Random;

namespace SphereWorld.Environment.Wind
{
    public class WindSimulation : MonoInstaller, ILoadAtStart
    {
        //[SerializeField][Tooltip("can apply only power of two")] private int resolution;
        //[SerializeField] private int depth;
        [SerializeField] private int particlesCount;
        [SerializeField] private ComputeShader mainShader;
        [SerializeField] private AnimationCurve outputPressure;
        [SerializeField] private float particleInfluenceSize;
        //[SerializeField] private float particleNearInfluencePercent;
        [SerializeField] private float cellSize;
        [SerializeField] private float simulationDeltaTime;
        [SerializeField] private float airGravity;
        [SerializeField] private float pushForce;
        [SerializeField] private float surfacePushForce;
        [SerializeField] private float zeroHeightPressure;
        [SerializeField] private float heightMul;
        [SerializeField] private float energyGrowthFactor;
        [SerializeField] private float energyLossFactor;
        //[SerializeField] private float nearPushForce;
        [SerializeField] private float viscosity;
        //[SerializeField] private float nearViscosity;
        [SerializeField] private float solPeriod;
        [SerializeField] private float yearPeriod;
        [SerializeField] private float sunlightInclination;
        [SerializeField] private GPUNoiseParameters pressureNoise;
        [SerializeField][FolderPath] private string saveLoadFilePath;
        [SerializeField] private bool needLoadAtStart;
        [SerializeField] private RenderTexture image;
        [SerializeField] private int ticksPerFrame;
        [Header("debug")]
        [SerializeField] private int selectedParticle;
        [SerializeField] private int3 selectedCell;
        [SerializeField] private int drawParticlesParts;
        [SerializeField] private int readDataPeriod;
        [SerializeField] private bool enableDebug;
        [SerializeField] private Transform sunlightIndicator;
        [SerializeField] private Vector2 pressureVisualization;
        [SerializeField] private Vector2 energyVisualization;

        [ShowInInspector] private List<AnchorParticle> _anchorParticles = new ();
        [ShowInInspector] private float mediumPressure;
        [ShowInInspector] private float mediumEnergy;
        private AtmosphereProfile _atmosphereProfile;
        public event Action OnSimulationTickComplete;
        
        //[ShowInInspector] private List<Particle> particlesToShow;
        private ComputeBuffer _particlesBuffer;
        private ComputeBuffer _gridElementsBuffer;
        private ComputeBuffer _gridBuffer;
        private ComputeBuffer _gridCounterBuffer;
#if DEBUG_ENABLED
        private ComputeBuffer _collisionsCounterBuffer;
        private ComputeBuffer _collisionsDebugBuffer;
#endif
        private ComputeBuffer _noiseParametersBuffer;
        //private RenderTexture[] _surfaces;
        private Particle[] _particlesOutput;
#if DEBUG_ENABLED
        private int[] _collisionsOutput;
        private int[] _collisionsOutputCount;
#endif
        private int[] _gridData;
        private int2[] _elements;
        private int _anchorsParticlesCount;
        Particle[] _singleParticle = new Particle[1];

        private WorldProfile _worldProfile;
        // ReSharper disable InconsistentNaming
        private readonly int particles = Shader.PropertyToID("particles");
        private readonly int elements = Shader.PropertyToID("elements");
        private readonly int grid = Shader.PropertyToID("grid");
        private readonly int counter = Shader.PropertyToID("counter");
        private readonly int collisions_counter = Shader.PropertyToID("collisions_counter");
        private readonly int noise_parameters = Shader.PropertyToID("noise_parameters");
        private readonly int collisions_debug = Shader.PropertyToID("collisions_debug");
        private readonly int min_radius_sqr = Shader.PropertyToID("min_radius_sqr");
        private readonly int max_radius_sqr = Shader.PropertyToID("max_radius_sqr");
        private readonly int cell_coord_mul = Shader.PropertyToID("cell_coord_mul");
        private readonly int energy_growth_factor = Shader.PropertyToID("energy_growth_factor");
        private readonly int energy_loss_factor = Shader.PropertyToID("energy_loss_factor");
        private readonly int grid_length = Shader.PropertyToID("grid_length");
        private readonly int dispatch_max_index = Shader.PropertyToID("dispatch_max_index");
        private readonly int entity_particle_start_index = Shader.PropertyToID("entity_particle_start_index");
        private readonly int elements_length = Shader.PropertyToID("elements_length");
        private readonly int gravity = Shader.PropertyToID("gravity");
        private readonly int sunlight = Shader.PropertyToID("sunlight");
        private readonly int viscosity_coefficient = Shader.PropertyToID("viscosity_coefficient");
        private readonly int push_force = Shader.PropertyToID("push_force");
        private readonly int surface_push_force = Shader.PropertyToID("surface_push_force");
        private readonly int zero_height_pressure = Shader.PropertyToID("zero_height_pressure");
        private readonly int height_mul = Shader.PropertyToID("height_mul");
        private readonly int cell_coord_offset = Shader.PropertyToID("cell_coord_offset");
        private readonly int max_grid_side_size = Shader.PropertyToID("max_grid_side_size");
        private readonly int particle_influence_radius = Shader.PropertyToID("particle_influence_radius");
        private readonly int delta_time = Shader.PropertyToID("delta_time");
        private readonly int collisions_debug_origin_index = Shader.PropertyToID("collisions_debug_origin_index");
        private readonly int outputImage = Shader.PropertyToID("outputImage");
        // ReSharper restore InconsistentNaming
        
        bool ILoadAtStart.enabled => enabled && gameObject.activeInHierarchy;

        public Task Load()
        {
            Initialize();
            return Task.CompletedTask;
        }
        
        private void OnValidate()
        {
            int power = Mathf.RoundToInt(Mathf.Log(particlesCount, 2));
            particlesCount = (int)Mathf.Pow(2, power);
            if (Application.isPlaying && _particlesBuffer != null)
            {
                SetProperties();
            }
        }

        public override void InstallBindings()
        {
            Container.Bind<RenderTexture>().WithId("_TexWindow").FromInstance(image);
            Container.Bind<WindSimulation>().FromInstance(this);
        }

        public void Initialize()
        {
            _worldProfile = Container.Resolve<WorldProfile>();
            _atmosphereProfile = _worldProfile.GetChildAssets<AtmosphereProfile>().First();
            outputPressure.ClearKeys();
            for (float i = 0; i < _worldProfile.atmosphereDepthKilometers * 1000; i += 1000)
            {
                var keyframe = new Keyframe(i, _atmosphereProfile.EvaluatePressurePercent(_worldProfile.gravity, i));
                outputPressure.AddKey(keyframe);
            }

            for (int i = 0; i < outputPressure.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(outputPressure, i,
                    AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(outputPressure, i,
                    AnimationUtility.TangentMode.Auto);
            }
            
            MakeParticlesBuffer();
            if (!image)
            {
                image = new RenderTexture(1024, 1024, GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormat.None, 1)
                    { enableRandomWrite = true };
                image.Create();
            }

            float maxGridRadius = 1f + _worldProfile.atmosphereDepthKilometers / _worldProfile.rigidPlanetRadiusKilometers;
            MakeGridAndLinks(maxGridRadius, out int linksCount, out float maxRadiusSqr, out float minRadiusSqr, out float cellCoordMul);
            mainShader.SetFloat(min_radius_sqr, minRadiusSqr);
            mainShader.SetFloat(max_radius_sqr, maxRadiusSqr);
            mainShader.SetFloat(cell_coord_mul, cellCoordMul);
            mainShader.SetFloat(max_grid_side_size, GetMaxGridSideSize(maxGridRadius, out _));
            PrepareBuffersForKernel(0);
            PrepareBuffersForKernel(1);
            PrepareBuffersForKernel(2);
            PrepareBuffersForKernel(3);
            PrepareBuffersForKernel(4);
            PrepareBuffersForKernel(5);
            mainShader.SetTexture(5, outputImage, image);

            SetProperties();

            StructureUpdateModule.OnEndUpdateTick += OnTick;
            _routine = TickSimulation();

            //TickSimulation();

            // _surfaces[i] = new RectangleSimulationSurface(Mathf.RoundToInt(Mathf.Sqrt(resolution)), depth);
            /*_surfaces = new RenderTexture[6];
            int sideSize = Mathf.RoundToInt(Mathf.Sqrt(resolution));
            for (var i = 0; i < _surfaces.Length; i++)
            {
                _surfaces[i] = new RenderTexture(sideSize, sideSize, depth);
            }*/
        }

        private void SetProperties()
        {
            mainShader.SetFloat(delta_time, simulationDeltaTime);
            mainShader.SetFloat(gravity, airGravity);
            mainShader.SetFloat(cell_coord_offset, cellSize * 0.5f);
            mainShader.SetFloat(energy_growth_factor, energyGrowthFactor);
            mainShader.SetFloat(energy_loss_factor, energyLossFactor);
            mainShader.SetFloat(push_force, pushForce);
            mainShader.SetFloat(surface_push_force, surfacePushForce);
            mainShader.SetFloat(zero_height_pressure, zeroHeightPressure);
            mainShader.SetFloat(height_mul, heightMul);
            mainShader.SetFloat(particle_influence_radius, particleInfluenceSize);
            mainShader.SetFloat(viscosity_coefficient, viscosity);
            mainShader.SetInt(elements_length, _gridElementsBuffer.count);
            mainShader.SetInt(grid_length, _gridBuffer.count);
            mainShader.SetInt(collisions_debug_origin_index, selectedParticle);
            mainShader.SetInt(entity_particle_start_index, particlesCount - _anchorsParticlesCount);
            _noiseParametersBuffer.SetData(new[]{pressureNoise});
        }

        private Queue<(Vector3 pos, Vector3 vel)> _entitiesToAdd = new();
        public AnchorParticle AddAnchor(Vector3 position, Vector3 velocity)
        {
            _entitiesToAdd.Enqueue((position, velocity));
            AnchorParticle particle = new AnchorParticle();
            particle.index = _anchorsParticlesCount + _entitiesToAdd.Count - 1;
            particle.height = (position.magnitude - 1) * _worldProfile.rigidPlanetRadiusKilometers * 1000;
            particle.balloonPressure = _atmosphereProfile.EvaluatePressurePercent(_worldProfile.gravity, particle.height);
            _anchorParticles.Add(particle);
            return particle;
        }

        public Particle GetAnchor(int idx)
        {
            _particlesBuffer.GetData(_singleParticle, 0, particlesCount - idx - 1, 1);
            return _singleParticle[0];
        }
        
        private IEnumerator _routine;
        
        private void OnTick()
        {
            for (int i = 0; i < ticksPerFrame; i++)
            {
                _routine.MoveNext();
            }
        }
        
        private IEnumerator TickSimulation()
        {
            while (enabled)
            {
                float solPhase = Time.time / solPeriod;
                float yearPhase = Time.time / yearPeriod;
                Quaternion rotation = Quaternion.Euler(0, solPhase * 90, 0);
                Quaternion inclination = Quaternion.Euler(Mathf.Cos(yearPhase * Mathf.PI * 0.5f) * sunlightInclination, 0, 0);
                Vector3 sunlightDirection = rotation * inclination * Vector3.forward;
                sunlightIndicator.position = -sunlightDirection * 2;
                sunlightIndicator.rotation = rotation * inclination;
                mainShader.SetVector(sunlight, sunlightDirection);
                if (_entitiesToAdd.Count > 0)
                {
                    while (_entitiesToAdd.TryDequeue(out (Vector3 pos, Vector3 vel) value))
                    {
                        _singleParticle[0].SetPosition(value.pos);
                        _singleParticle[0].SetVelocity(value.vel);
                        _singleParticle[0].gridIndex = -1;
                        _anchorsParticlesCount++;
                        _particlesBuffer.SetData(_singleParticle, 0, particlesCount - _anchorsParticlesCount, 1);
                    }
                }

                ClearGrid();
                UpdateGrid();
                yield return null;
                CalculatePressure();
                yield return null;
                FindGradient();
                yield return null;
                OverrideDensities();
                ReadData();
                /*bool exception = false;
                for (var i = 0; i < _collisionsOutputCount[0]; i += 2)
                {
                    int a = _collisionsOutput[i];
                    int b = _collisionsOutput[i+1];
                    var pa = _particlesOutput[a].GetPosition();
                    var pb = _particlesOutput[b].GetPosition();
                    Debug.DrawLine(pa, pa * 1.01f, Color.red, 10);
                    Debug.DrawLine(pb, pb * 1.01f, Color.red, 10);
                    Debug.DrawLine(pb * 1.01f, pa * 1.01f, Color.cyan, 10);
                    Debug.Log($"Collides {a} and {b}");
                    exception = true;
                    //int3 cell = CoordFromIndex(_particlesOutput[a].gridIndex, cellsPerSide);
                    //DrawCell(cellsPerSideHalf, cellOffset, cell, Color.cyan);
                }

                if (exception)
                {
                    enabled = false;
                    EditorApplication.isPaused = true;
                }*/
                /*mainShader.SetFloat(push_force, nearPushForce);
                mainShader.SetFloat(particle_influence_radius, particleInfluenceSize * particleNearInfluencePercent);
                mainShader.SetFloat(viscosity_coefficient, nearViscosity);
                CalculatePressure();
                FindGradient();*/
                //ModifyPressure();
                MoveParticles();
                yield return null;
                OnSimulationTickComplete?.Invoke();
                yield return null;
                DrawPixels();
                yield return null;
            }
        }

        private void OverrideDensities()
        {
            Particle[] arr = new Particle[1];
            foreach (var particle in _anchorParticles)
            {
                _particlesBuffer.GetData(arr, 0, particlesCount - _anchorsParticlesCount + particle.index, 1);
                arr[0].density = mediumPressure;
                arr[0].energy = mediumEnergy;
                //arr[0].gradient = Vector3.ProjectOnPlane(arr[0].gradient, arr[0].position);
                float pMag = arr[0].position.magnitude;
                Vector3 pNorm = arr[0].position / pMag;
                double height = (pMag - 1d) * _worldProfile.rigidPlanetRadiusKilometers * 1000d;
                double pressure = _atmosphereProfile.EvaluatePressurePercent(_worldProfile.gravity, height);
                particle.verticalVelocity += (pressure - particle.balloonPressure) * particle.balloonVolume * _worldProfile.gravity / particle.balloonMass * StructureUpdateModule.DeltaTime;
                particle.verticalVelocity -= particle.verticalVelocity * particle.verticalDrag * StructureUpdateModule.DeltaTime;
                height += particle.verticalVelocity * StructureUpdateModule.DeltaTime;
                particle.height = height;
                Debug.DrawRay(arr[0].position, pNorm * 0.03f, Color.red);
                arr[0].velocity = Vector3.ProjectOnPlane(arr[0].velocity, pNorm);
                Debug.DrawRay(arr[0].position, arr[0].velocity, Color.cyan);
                arr[0].position = pNorm * (float)(1d + height / (_worldProfile.rigidPlanetRadiusKilometers * 1000d));
                
                /*Vector3 p = arr[0].GetPosition();
                arr[0].SetPosition(p.normalized * (1 + )); */
                
                _particlesBuffer.SetData(arr, 0, particlesCount - _anchorsParticlesCount + particle.index, 1);
            }
        }

        private void ClearGrid()
        {
            mainShader.SetInt(dispatch_max_index, _gridElementsBuffer.count);
            mainShader.Dispatch(0, new int3{x = _gridElementsBuffer.count, y = 1, z = 1});
        }
        private void UpdateGrid()
        {
            mainShader.SetInt(dispatch_max_index, particlesCount);
            mainShader.Dispatch(1, new int3{x = particlesCount, y = 1, z = 1});
        }
        
        private void CalculatePressure()
        {
            mainShader.SetInt(dispatch_max_index, particlesCount);
            mainShader.Dispatch(2, new int3{x = particlesCount, y = 1, z = 1});
        }

        private void FindGradient()
        {
            mainShader.SetInt(dispatch_max_index, particlesCount);
            mainShader.Dispatch(3, new int3{x = particlesCount, y = 1, z = 1});
        }

        private void MoveParticles()
        {
            mainShader.SetInt(dispatch_max_index, particlesCount);
            mainShader.Dispatch(4, new int3{x = particlesCount, y = 1, z = 1});
        }
        
        private void DrawPixels()
        {
            mainShader.SetInt(dispatch_max_index, 1024);
            mainShader.Dispatch(5, new int3{x = 2048, y = 1024, z = 1});
        }

        private void PrepareBuffersForKernel(int kernelIndex)
        {
            mainShader.SetBuffer(kernelIndex, particles, _particlesBuffer);
            mainShader.SetBuffer(kernelIndex, elements, _gridElementsBuffer);
            mainShader.SetBuffer(kernelIndex, grid, _gridBuffer);
            mainShader.SetBuffer(kernelIndex, counter, _gridCounterBuffer);
#if DEBUG_ENABLED
            mainShader.SetBuffer(kernelIndex, collisions_counter, _collisionsCounterBuffer);
            mainShader.SetBuffer(kernelIndex, collisions_debug, _collisionsDebugBuffer);
#endif
            mainShader.SetBuffer(kernelIndex, noise_parameters, _noiseParametersBuffer);
        }
        
        [Button]
        private void ReadData()
        {
            _particlesBuffer.GetData(_particlesOutput);
            
            if (enableDebug)
            {
#if DEBUG_ENABLED
                 _collisionsDebugBuffer.GetData(_collisionsOutput);
                 _collisionsCounterBuffer.GetData(_collisionsOutputCount);
#endif
                //_gridBuffer.GetData(_gridData);
                //_gridElementsBuffer.GetData(_elements);
            }
        }

        /*[Button]
        private void AnalizeCells()
        {
            Dictionary<int, List<int>> particlesPerCell = new ();
            HashSet<int> distributedParticles = new HashSet<int>();
            float maxGridRadius = 1f + _worldProfile.atmosphereDepthKilometers / _worldProfile.rigidPlanetRadiusKilometers;
            int cellsPerSide = GetMaxGridSideSize(maxGridRadius, out int _);
            int gridCell = IndexFromCoord(selectedCell.x, selectedCell.y, selectedCell.z, cellsPerSide);
            particlesToShow = new List<Particle>();
            if (_gridData[gridCell] != -1)
            {
                int iterator = _gridData[gridCell];
                while (iterator != -1)
                {
                    int2 element = _elements[iterator];
                    iterator = element.y;
                    
                    particlesToShow.Add(_particlesOutput[element.x]);
                }
            }
        }*/

       /*[Button]
       private void AnalizeCell()
       {
           float step = (pressureMinMax.y - pressureMinMax.x) / 20;
           int[] steps = new int[20];
           
           for (var i = 0; i < _particlesOutput.Length; i++)
           {
               
           }
       }*/
#if DEBUG_ENABLED

        private int drawCounter = 0;
        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && !EditorApplication.isPaused)
            {
                mediumEnergy = 0;
                mediumPressure = 0;
                float particlesCountDivider = 1f / particlesCount;
                float maxGridRadius = 1f + _worldProfile.atmosphereDepthKilometers / _worldProfile.rigidPlanetRadiusKilometers;
                Gizmos.DrawWireSphere(Vector3.zero, maxGridRadius);
                Gizmos.DrawWireSphere(Vector3.zero, 1);
                float cellOffset = cellSize * 0.5f;
                int cellsPerSide = GetMaxGridSideSize(maxGridRadius, out int cellsPerSideHalf);
                int selectedParticleGridIndex = -1;
                var cameraDirection = Camera.current.transform.position;
                if (_particlesOutput != null)
                {
                    for (var index = 0; index < _particlesOutput.Length; index++)
                    {
                        var particle = _particlesOutput[index];
                        float pressure = (particle.density - pressureVisualization.y) / pressureVisualization.x;
                        float energy = (particle.energy - energyVisualization.y) / energyVisualization.x;
                        mediumEnergy += particle.energy * particlesCountDivider;
                        mediumPressure += particle.density * particlesCountDivider;
                        if (index == selectedParticle && enableDebug)
                        {
                            Vector3 p = particle.position;
                            for (int i = 0; i < Mathf.Min(_collisionsOutputCount[0], _collisionsOutput.Length); i++)
                            {
                                Debug.DrawLine(p, _particlesOutput[_collisionsOutput[i]].position, Color.yellow);
                            }
                            
                            Debug.DrawLine(p, p - particle.velocity * 0.0005f,  Color.green);
                            Debug.DrawLine(p, p * 1.008f, Color.red);
        
                            int3 cell = CoordFromIndex(particle.gridIndex, cellsPerSide);
                            DrawCell(cellsPerSideHalf, cellOffset, cell, Color.yellow);
                        }
                        else if(index % drawParticlesParts == drawCounter % drawParticlesParts)
                        {
                            Vector3 p = particle.position;
                            if (Vector3.Dot(cameraDirection, p) < 0.3f)
                            {
                                continue;
                            }
        
                            
                            Vector3 v = particle.velocity * 0.0015f;
                            //float n = Vector3.Dot(v.normalized, Vector3.forward);
                            //float d = Vector3.Dot(particle.GetVelocity().normalized, particle.GetPosition().normalized) * 0.5f + 0.5f;
                            Color c = Color.Lerp(Color.green, Color.red, energy);// * (0.6f + d * 0.4f);
                            //c = Color.Lerp(c, new Color(.6f, .8f, 1), d);
                            Debug.DrawLine(p - v, p + v, c, Time.deltaTime * drawParticlesParts);
                            //Debug.DrawLine(p, p * 1.008f, Color.red * 0.5f, Time.deltaTime * drawParticlesParts);
//                            Gizmos.DrawSphere(particle.GetPosition(), 0.01f);
                        }
        
                        
                    }
                }
        
        
                if (enableDebug)
                {
                    
                }
                
                //int3 coord = CoordFromIndex(selectedCell, cellsPerSide);
                DrawCell(cellsPerSideHalf, cellOffset, selectedCell, Color.red);
                /*int counter = 0;
                foreach (var coord in EnumerateValidCells(maxGridRadius, out _, out _, out float cellCoordMul,
                             out int cellsPerSide))
                {
                    int gridIndex = IndexFromCoord(coord.x, coord.y, coord.z, cellsPerSide);
                    if (counter++ == selectedCell)
                    {
                        Gizmos.color = Color.red;
                    }
                    else if (selectedParticleGridIndex == gridIndex)
                    {
                        Gizmos.color = Color.cyan;
                    }
                    else
                    {
                        Gizmos.color = Color.HSVToRGB((float)((coord.x + coord.y + coord.z) % 30) / 30, 1f,1f) * 0.2f;
                    }
                    var center = new Vector3(((float)coord.x - cellsPerSideHalf) * cellCoordMul + cellOffset,
                        ((float)coord.y - cellsPerSideHalf) * cellCoordMul + cellOffset,
                        ((float)coord.z - cellsPerSideHalf) * cellCoordMul + cellOffset);
                    Gizmos.DrawWireCube(center, Vector3.one * cellSize);
                }*/
        
                drawCounter++;
            }
        }
#endif


        private void DrawCell(int cellsPerSideHalf, float cellOffset, int3 cell, Color color)
        {
            Gizmos.color =color;
            float cellCoordMul = 1f / (cellsPerSideHalf - 1);
            var center = new Vector3(((float)cell.x - cellsPerSideHalf) * cellCoordMul + cellOffset,
                ((float)cell.y - cellsPerSideHalf) * cellCoordMul + cellOffset,
                ((float)cell.z - cellsPerSideHalf) * cellCoordMul + cellOffset);
            Gizmos.DrawWireCube(center, Vector3.one * cellSize);
        }

        private void MakeParticlesBuffer()
        {
            var particleSize = Marshal.SizeOf(typeof(Particle));
            if (needLoadAtStart)
            {
                using (FileStream stream = File.Open(saveLoadFilePath + "/Save.bin", FileMode.Open))
                {
                    GCHandle gcHandle;
                    byte[] header = new byte[4];
                    byte[] particleCache = new byte[particleSize];
                    stream.Read(header);
                    particlesCount = System.BitConverter.ToInt32(header);
                    _particlesOutput = new Particle[particlesCount];
                    for (int i = 0; i < particlesCount; i++)
                    {
                        stream.Read(particleCache);
                        gcHandle = GCHandle.Alloc(particleCache, GCHandleType.Pinned);
                        _particlesOutput[i] = ReadAs<Particle>(gcHandle);
                        gcHandle.Free();
                    }
                }

            }
            else
            {
                _particlesOutput = new Particle[particlesCount];
                for (int i = 0; i < particlesCount; i++)
                {
                    _particlesOutput[i].Randomize();
                }
            }
            _particlesBuffer = new ComputeBuffer(particlesCount, particleSize);
            _particlesBuffer.SetData(_particlesOutput);
        }
        
        [Button]
        private void SerializeParticlesBinary()
        {
            _particlesBuffer.GetData(_particlesOutput);
            using (FileStream stream = File.Open(saveLoadFilePath + "/Save.bin", FileMode.OpenOrCreate))
            {
                stream.Write(System.BitConverter.GetBytes(particlesCount));
                for (int i = 0; i < particlesCount; i++)
                {
                    var particle = _particlesOutput[i];
                    WriteObject(ref particle, stream);
                }
            }
        }
        
        private unsafe void WriteObject<T>(ref T obj, Stream stream) where T : unmanaged
        {
            int objectSize = Marshal.SizeOf(obj.GetType());
        
            fixed(void* pObject = &obj)
            {
                byte* bytePointer = (byte*)pObject;
            
                for (int i = 0; i < objectSize; ++i)
                {
                    stream.Write(new byte[] { bytePointer[i] }, 0, 1);
                }
            }
        }
        
        private T ReadAs<T>(GCHandle gcHandle)
        {
            T payload = (T)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(T));
            return payload;
        }

        private int GetMaxGridSideSize(float maxGridRadius, out int cellsOnSideCount)
        {
            cellsOnSideCount = Mathf.FloorToInt((maxGridRadius) / cellSize + 0.5f) + 1;
            return cellsOnSideCount * 2;
        }
        
        private void MakeGridAndLinks(float maxGridRadius, out int linksCount, out float maxRadiusSqr, out float minRadiusSqr, out float cellCoordMul)
        {
            maxRadiusSqr = (maxGridRadius + cellSize) * (maxGridRadius + cellSize);
            minRadiusSqr = (1 - cellSize) * (1 - cellSize);
            float cellOffset = cellSize * 0.5f;
            int cellsPerSide = GetMaxGridSideSize(maxGridRadius, out int cellsPerSideHalf);
            cellCoordMul = 1f / (cellsPerSideHalf - 1);
            _gridElementsBuffer = new ComputeBuffer(particlesCount,  8);
            _gridBuffer = new ComputeBuffer(cellsPerSide * cellsPerSide * cellsPerSide, 4);
            _gridCounterBuffer = new ComputeBuffer(1, 4);
            _noiseParametersBuffer = new ComputeBuffer(1, GPUNoiseParameters.SizeInBytes);
#if DEBUG_ENABLED
            _collisionsCounterBuffer = new ComputeBuffer(1, 4);
            _collisionsDebugBuffer = new ComputeBuffer(particlesCount, 4);
            _collisionsOutput = new int[particlesCount];
            _collisionsOutputCount = new int[1];    
#endif

            _gridData = new int[cellsPerSide * cellsPerSide * cellsPerSide];
            _elements = new int2[particlesCount];
            linksCount = 0;
        }


        private IEnumerable<int3> EnumerateValidCells(int cellsPerSide, int cellsPerSideHalf,
            float cellOffset, float maxRadiusSqr, float minRadiusSqr, float cellCoordMul)
        {
            for (int x = 0; x < cellsPerSide; x++)
            {
                for (int y = 0; y < cellsPerSide; y++)
                {
                    for (int z = 0; z < cellsPerSide; z++)
                    {
                        var center = new Vector3(((float)x - cellsPerSideHalf) * cellCoordMul + cellOffset,
                            ((float)y - cellsPerSideHalf) * cellCoordMul + cellOffset,
                            ((float)z - cellsPerSideHalf) * cellCoordMul + cellOffset);
                        if (IsCellValid(center, minRadiusSqr, maxRadiusSqr))
                        {
                            yield return new int3(x, y, z);
                        }
                    }
                }
            }
        }

        private int IndexFromCoord(int x, int y, int z, int size)
        {
            return x * size * size + y * size + z;
        }
        
        private int3 CoordFromIndex(int index, int size)
        {
            int x = index / (size * size);
            int r = index % (size * size);
            return new int3(x, r / size, r % size);
        }

        private bool IsCellValid(Vector3 center, float minRadiusSqr, float maxRadiusSqr)
        {
            float mag = center.sqrMagnitude;
            return mag > minRadiusSqr && mag < maxRadiusSqr;
        }

        private void OnDestroy()
        {
            _particlesBuffer?.Dispose();
            _gridElementsBuffer?.Dispose();
            _gridBuffer?.Dispose();
            _gridCounterBuffer?.Dispose();
#if DEBUG_ENABLED
            _collisionsCounterBuffer?.Dispose();
            _collisionsDebugBuffer?.Dispose();
#endif
            image?.Release();
        }
    }
}