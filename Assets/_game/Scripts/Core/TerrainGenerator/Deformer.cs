using System;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Core.Data;
using Core.Game;
using Core.TerrainGenerator.Settings;
using Core.TerrainGenerator.Utility;
using Core.Utilities;
using Core.World;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Runtime;

namespace Core.TerrainGenerator
{
    public class Deformer : MonoBehaviour, IDeformer
    {
        public T GetModules<T>() where T : class, IDeformerModule
        {
            Type t = typeof(T);
            foreach (SerializedDeformerModule module in modules)
            {
                module.Module.Init(this);
                if(module.GetLayerType() == t) return module.Module as T;
            }

            return null;
        }

        public Quaternion Rotation => transform.rotation;
        public Vector3 Position => transform.position - WorldOffset.Offset;
        public Rect AxisAlignedRect => CalculateAxisAlignedRect();

        public Vector4 LocalRect => localRect;
        public float Fade => fade;
        public int Layer => layer;

        [SerializeField] private Vector4 localRect;
        [SerializeField, Range(0f, 1f)] private float fade = 0.2f;
        [SerializeField] private int layer;

        [SerializeField] private List<SerializedDeformerModule> modules;

        [ShowInInspector] private Rect axisAlignedRect;
        [ShowInInspector] private Dictionary<int, List<Vector2Int>> affectedChunks = new Dictionary<int, List<Vector2Int>>();

        /*[SerializeField, HideInInspector]
        private string[] typesInfo;

        [SerializeField, HideInInspector]
        private string[] jsonConfig;*/

        public void Start()
        {
            CalculateAxisAlignedRect();
            foreach (SerializedDeformerModule module in modules)
            {
                module.Init(this);
            }
            TerrainProvider.OnInitialize.Subscribe(Register);
        }

        private void Register(TerrainProvider provider)
        {
            provider.RegisterDeformer(this);
        }

        [Button]
        public void AddDeformerSettings(System.Type deformer)
        {
            /*IDeformerLayerSettings layerSettings = System.Activator.CreateInstance(deformer) as IDeformerLayerSettings;
            layerSettings.Init(this);
            Settings.Add(layerSettings);*/
            modules.Add(new SerializedDeformerModule(this, deformer));
        }

        /*[Button]
        public void SaveToJson()
        {
            if (modules == null) return;
            
            typesInfo = new string[modules.Count];
            jsonConfig = new string[modules.Count];
            for (int i = 0; i < modules.Count; i++)
            {
                typesInfo[i] = modules[i].GetType().FullName;
                jsonConfig[i] = JsonConvert.SerializeObject(modules[i]);
            }
        }*/

        /*[Button]
        public void ReadFromJson()
        {
            settings = new List<IDeformerLayerSettings>();
            if(jsonConfig == null || jsonConfig.Length == 0) return;
            
            for(int i = 0; i < jsonConfig.Length; i++)
            {
                System.Type type = TypeExtensions.GetTypeByName(typesInfo[i]);
                IDeformerLayerSettings newDeformer = JsonConvert.DeserializeObject(jsonConfig[i], type) as IDeformerLayerSettings;
                newDeformer.Init(this);
                settings.Add(newDeformer);
            }
        }*/

        [Button]
        public void ReadFromTerrain()
        {
            foreach (SerializedDeformerModule deformerLayerSettings in modules)
            {
                deformerLayerSettings.Module.Init(this);
                deformerLayerSettings.Module.ReadFromTerrain();
                deformerLayerSettings.Serialize();
            }

            foreach (SerializedDeformerModule module in modules)
            {
                module.Serialize();
            }
        }

        private Vector3 lastCalculationPosition;
        private Quaternion lastCalculationRotation;
        public Rect CalculateAxisAlignedRect()
        {
            Vector3 position = Position;
            Quaternion rotation = transform.rotation;
            if(lastCalculationPosition == position && lastCalculationRotation == rotation) return axisAlignedRect;

            lastCalculationPosition = position;
            lastCalculationRotation = rotation;

            (Vector3 min, Vector3 max) = MathfUtilities.GetAxisAlignedRect(localRect, rotation, position);
            axisAlignedRect.min = min;
            axisAlignedRect.max = max;
            return axisAlignedRect;
        }


        public IEnumerable<Vector2Int> GetAffectChunks(float chunkSize)
        {
            int key = (int) chunkSize;
            if (affectedChunks.TryGetValue(key, out List<Vector2Int> chunks)) return chunks;
            
            Vector4 rect = LocalRect;
            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            Vector3 position = Position;
            affectedChunks.Add(key, MathfUtilities.GetAffectChunks(chunkSize, right, rect, forward, position));

            return affectedChunks[key];
        }

        public Terrain[] GetTerrainsContacts() //TODO: Cache
        {
            Vector4 rect = LocalRect;
            Transform tr = transform;
            Vector3 right = tr.right;
            Vector3 forward = tr.forward;
            Vector3 position = tr.position;
            return MathfUtilities.GetTerrainsContacts(right, rect, forward, position);
        }

        public Vector2 GetLocalPointCoordinates(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos + WorldOffset.Offset);

            float x = localPos.x;
            float y = localPos.z;

            Vector4 lRect = LocalRect;

            x = (x - lRect.x) + lRect.z * 0.5f;
            y = (y - lRect.y) + lRect.w * 0.5f;

            x /= lRect.z;
            y /= lRect.w;

            return new Vector2(x, y);
        }

        public Vector3 InverseTransformPoint(Vector3 worldPos) => transform.InverseTransformPoint(worldPos + WorldOffset.Offset);
        public Vector3 TransformPoint(Vector3 localPos) => transform.TransformPoint(localPos - WorldOffset.Offset);
        public virtual void OnSetDirty(IDeformerModule dirtyModule)
        {
            if (dirtyModule is HeightMapDeformerModule heightMapDeformerModule && heightMapDeformerModule.alignWithTerrain)
            {
                AlignWithTerrain();
            }
        }

        public void AlignWithTerrain()
        {
            Vector3 origin = transform.position;
            float height = TerrainProvider.MaxWorldHeight;
            origin.y = height;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, height, GameData.Data.terrainLayer))
            {
                transform.position = hit.point;
            }
        }


        private void OnDrawGizmosSelected()
        {
            CalculateAxisAlignedRect();

            Gizmos.DrawWireCube(new Vector3(axisAlignedRect.position.x + axisAlignedRect.width * 0.5f, transform.position.y, axisAlignedRect.position.y + axisAlignedRect.height * 0.5f), new Vector3(axisAlignedRect.width, 0, axisAlignedRect.height));

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.white * 0.8f;
            Gizmos.DrawWireCube(new Vector3(localRect.x, 0, localRect.y), new Vector3(localRect.z, 0, localRect.w));
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(new Vector3(localRect.x, 0, localRect.y), new Vector3(localRect.z, 0, localRect.w) * (1 - fade));
        }
        

    }
}
