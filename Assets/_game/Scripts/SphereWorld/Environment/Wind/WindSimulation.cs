using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Utilities;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace SphereWorld.Environment.Wind
{
    public class WindSimulation : MonoBehaviour, ILoadAtStart
    {
        //[SerializeField][Tooltip("can apply only power of two")] private int resolution;
        //[SerializeField] private int depth;
        [SerializeField] private int particlesCount;
        [SerializeField] private ComputeShader mainShader;
        [SerializeField] private AnimationCurve outputPressure;
        [SerializeField] private float particleInfluenceSize;
        [SerializeField] private float simulationDeltaTime;
        [SerializeField] private int selectedParticle;
        [SerializeField] private int selectedCell;
        private ComputeBuffer _particlesBuffer;
        private ComputeBuffer _gridElementsBuffer;
        private ComputeBuffer _gridBuffer;
        private ComputeBuffer _gridCounterBuffer;
        private ComputeBuffer _gridLinksBuffer;
        //private RenderTexture[] _surfaces;
        private Particle[] _particlesOutput;

        [Inject] private WorldProfile _worldProfile;
        // ReSharper disable InconsistentNaming
        private readonly int particles = Shader.PropertyToID("particles");
        private readonly int elements = Shader.PropertyToID("elements");
        private readonly int grid = Shader.PropertyToID("grid");
        private readonly int counter = Shader.PropertyToID("counter");
        private readonly int grid_links = Shader.PropertyToID("grid_links");
        private readonly int grid_links_count = Shader.PropertyToID("grid_links_count");
        private readonly int min_radius_sqr = Shader.PropertyToID("min_radius_sqr");
        private readonly int max_radius_sqr = Shader.PropertyToID("max_radius_sqr");
        private readonly int cell_coord_mul = Shader.PropertyToID("cell_coord_mul");
        private readonly int grid_length = Shader.PropertyToID("grid_length");
        private readonly int dispatch_max_index = Shader.PropertyToID("dispatch_max_index");
        private readonly int elements_length = Shader.PropertyToID("elements_length");
        private readonly int cell_coord_offset = Shader.PropertyToID("cell_coord_offset");
        private readonly int max_grid_side_size = Shader.PropertyToID("max_grid_side_size");
        private readonly int particle_influence_radius = Shader.PropertyToID("particle_influence_radius");
        private readonly int delta_time = Shader.PropertyToID("delta_time");
        // ReSharper restore InconsistentNaming
        
        private WorldProfile _profile;

        public Task Load()
        {
            Initialize(_worldProfile);
            return Task.CompletedTask;
        }
        
        private void OnValidate()
        {
            int power = Mathf.RoundToInt(Mathf.Log(particlesCount, 2));
            particlesCount = (int)Mathf.Pow(2, power);
        }

        public void Initialize(WorldProfile profile)
        {
            _profile = profile;
            AtmosphereProfile atmosphereProfile = profile.GetChildAssets<AtmosphereProfile>().First();
            outputPressure.ClearKeys();
            for (float i = 0; i < profile.atmosphereDepthKilometers * 1000; i += 1000)
            {
                var keyframe = new Keyframe(i, atmosphereProfile.EvaluatePressurePercent(profile.gravity, i));
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
            float maxGridRadius = 1f + _profile.atmosphereDepthKilometers / _profile.rigidPlanetRadiusKilometers;
            MakeGridAndLinks(maxGridRadius, out int linksCount, out float maxRadiusSqr, out float minRadiusSqr, out float cellCoordMul);
            
            mainShader.SetFloat(particle_influence_radius, particleInfluenceSize);
            mainShader.SetFloat(delta_time, simulationDeltaTime);
            mainShader.SetFloat(min_radius_sqr, minRadiusSqr);
            mainShader.SetFloat(max_radius_sqr, maxRadiusSqr);
            mainShader.SetFloat(cell_coord_mul, cellCoordMul);
            mainShader.SetFloat(cell_coord_offset, particleInfluenceSize * 0.5f);
            mainShader.SetFloat(max_grid_side_size, GetMaxGridSideSize(maxGridRadius, out _));
            mainShader.SetInt(grid_links_count, linksCount);
            mainShader.SetInt(elements_length, _gridBuffer.count);
            mainShader.SetInt(grid_length, _gridElementsBuffer.count);

            //TickSimulation();

            // _surfaces[i] = new RectangleSimulationSurface(Mathf.RoundToInt(Mathf.Sqrt(resolution)), depth);
            /*_surfaces = new RenderTexture[6];
            int sideSize = Mathf.RoundToInt(Mathf.Sqrt(resolution));
            for (var i = 0; i < _surfaces.Length; i++)
            {
                _surfaces[i] = new RenderTexture(sideSize, sideSize, depth);
            }*/
        }

       /* private int a = 0;
        private void Update()
        {
            TickSimulation();
            if (a++ % 100 == 0)
            {
                ReadData();
            }
        }*/
 //       [Button]
        private void TickSimulation()
        {
            ClearGrid();
            UpdateGrid();
            FindGradient();
            MoveParticles();
        }
        [Button]

        private void ClearGrid()
        {
            PrepareBuffersForKernel(0);
            mainShader.SetInt(dispatch_max_index, _gridElementsBuffer.count);
            mainShader.Dispatch(0, new int3{x = _gridElementsBuffer.count, y = 1, z = 1});
        }
        [Button]

        private void UpdateGrid()
        {
            PrepareBuffersForKernel(1);
            mainShader.SetInt(dispatch_max_index, particlesCount);
            mainShader.Dispatch(1, new int3{x = particlesCount, y = 1, z = 1});
        }
        [Button]

        private void FindGradient()
        {
            PrepareBuffersForKernel(2);
            mainShader.SetInt(dispatch_max_index, particlesCount);
            mainShader.Dispatch(2, new int3{x = particlesCount, y = 1, z = 1});
        }
        [Button]

        private void MoveParticles()
        {
            PrepareBuffersForKernel(3);
            mainShader.SetInt(dispatch_max_index, particlesCount);
            mainShader.Dispatch(3, new int3{x = particlesCount, y = 1, z = 1});
        }

        private void PrepareBuffersForKernel(int kernelIndex)
        {
            mainShader.SetBuffer(kernelIndex, particles, _particlesBuffer);
            mainShader.SetBuffer(kernelIndex, grid_links, _gridLinksBuffer);
            mainShader.SetBuffer(kernelIndex, elements, _gridElementsBuffer);
            mainShader.SetBuffer(kernelIndex, grid, _gridBuffer);
            mainShader.SetBuffer(kernelIndex, counter, _gridCounterBuffer);
        }
        
        [Button]
        private void ReadData()
        {
            _particlesBuffer.GetData(_particlesOutput);
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
            {
                float maxGridRadius = 1f + _profile.atmosphereDepthKilometers / _profile.rigidPlanetRadiusKilometers;
                Gizmos.DrawWireSphere(Vector3.zero, maxGridRadius);
                Gizmos.DrawWireSphere(Vector3.zero, 1);
                float cellOffset = particleInfluenceSize * 0.5f;
                GetMaxGridSideSize(maxGridRadius, out int cellsPerSideHalf);
                int selectedParticleGridIndex = -1;
                if (_particlesOutput != null)
                {
                    for (var index = 0; index < _particlesOutput.Length; index++)
                    {
                        var particle = _particlesOutput[index];
                        if (index == selectedParticle)
                        {
                            Gizmos.color = Color.red;
                            selectedParticleGridIndex = _particlesOutput[index].gridIndex;
                        }
                        else
                        {
                            Gizmos.color = Color.yellow * 0.7f;
                        }
                        Gizmos.DrawSphere(particle.GetPosition(), 0.01f);
                    }
                }
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
                    Gizmos.DrawWireCube(center, Vector3.one * particleInfluenceSize);
                }
*/
                
            }
        }

        private void MakeParticlesBuffer()
        {
            var particleSize = Marshal.SizeOf(typeof(Particle));
            _particlesBuffer = new ComputeBuffer(particlesCount, particleSize, ComputeBufferType.Structured);
            _particlesOutput = new Particle[particlesCount];
            for (int i = 0; i < particlesCount; i++)
            {
                _particlesOutput[i].Randomize();
            }
            _particlesBuffer.SetData(_particlesOutput);
        }

        private int GetMaxGridSideSize(float maxGridRadius, out int cellsOnSideCount)
        {
            cellsOnSideCount = Mathf.FloorToInt((maxGridRadius) / particleInfluenceSize + 0.5f) + 1;
            return cellsOnSideCount * 2;
        }
        
        private void MakeGridAndLinks(float maxGridRadius, out int linksCount, out float maxRadiusSqr, out float minRadiusSqr, out float cellCoordMul)
        {
            List<int> links = new ();
            foreach (var coord in EnumerateValidCells(maxGridRadius, out maxRadiusSqr, out minRadiusSqr, out cellCoordMul, out int cellsPerSide))
            {
                links.Add(IndexFromCoord(coord.x, coord.y, coord.z, cellsPerSide));
            }
            _gridLinksBuffer = new ComputeBuffer(links.Count, 4);
            _gridLinksBuffer.SetData(links);
            _gridElementsBuffer = new ComputeBuffer(links.Count,  4);
            _gridBuffer = new ComputeBuffer(particlesCount, 8);
            _gridCounterBuffer = new ComputeBuffer(1, 4);
            linksCount = links.Count;
        }

        private IEnumerable<int3> EnumerateValidCells(float maxGridRadius, out float maxRadiusSqr, out float minRadiusSqr, out float cellCoordMul, out int cellsPerSide)
        {
            maxRadiusSqr = (maxGridRadius + particleInfluenceSize) * (maxGridRadius + particleInfluenceSize);
            minRadiusSqr = (1 - particleInfluenceSize) * (1 - particleInfluenceSize);
            float cellOffset = particleInfluenceSize * 0.5f;
            cellsPerSide = GetMaxGridSideSize(maxGridRadius, out int cellsPerSideHalf);
            cellCoordMul = 1f / (cellsPerSideHalf - 1);
            return EnumerateValidCells(cellsPerSide, cellsPerSideHalf, cellOffset, maxRadiusSqr, minRadiusSqr, cellCoordMul);
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

        private bool IsCellValid(Vector3 center, float minRadiusSqr, float maxRadiusSqr)
        {
            float mag = center.sqrMagnitude;
            return mag > minRadiusSqr && mag < maxRadiusSqr;
        }

        private void OnDestroy()
        {
            _gridElementsBuffer.Dispose();
            _particlesBuffer.Dispose();
        }
    }
}