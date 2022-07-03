using System;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
using Core.TerrainGenerator.Utility;
using Core.Utilities;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace Core.TerrainGenerator
{
    public class Deformer : MonoBehaviour, IDeformer
    {
        public T GetModules<T>() where T : class, IDeformerModule
        {
            Type t = typeof(T);
            foreach (SerializedDeformerModule module in modules)
            {
                if(module.GetLayerType() == t) return module.Module as T;
            }

            return null;
        }

        public Quaternion Rotation => transform.rotation;
        public Vector3 Position => transform.position;
        public Rect AxisAlignedRect => axisAlignedRect;

        public Vector4 LocalRect => localRect;
        public float Fade => fade;
        public int Layer => layer;

        [SerializeField] private Vector4 localRect;
        [SerializeField, Range(0f, 1f)] private float fade = 0.2f;
        [SerializeField] private int layer;

        [SerializeField] private List<SerializedDeformerModule> modules;

        [ShowInInspector] private Rect axisAlignedRect;
        [ShowInInspector] private List<Vector2Int> affectedChunks;

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
            TerrainProvider.onInitialize.Subscribe(Register);
        }

        private void Register()
        {
            TerrainProvider.Instance.RegisterDeformer(this);
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


        private void CalculateAxisAlignedRect()
        {
            Quaternion rotation = transform.rotation;
            Vector3 position = transform.position; 
            (Vector3 min, Vector3 max) = MathfUtilities.GetAxisAlignedRect(localRect, rotation, position);
            axisAlignedRect.min = min;
            axisAlignedRect.max = max;
        }


        public IEnumerable<Vector2Int> GetAffectChunks(float chunkSize)
        {
            if (affectedChunks != null) return affectedChunks;
            
            Vector4 rect = LocalRect;
            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            Vector3 position = transform.position;
            affectedChunks = MathfUtilities.GetAffectChunks(chunkSize, right, rect, forward, position);

            return affectedChunks;
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
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            float x = localPos.x;
            float y = localPos.z;

            Vector4 lRect = LocalRect;

            x = (x - lRect.x) + lRect.z * 0.5f;
            y = (y - lRect.y) + lRect.w * 0.5f;

            x /= lRect.z;
            y /= lRect.w;

            return new Vector2(x, y);
        }

        public Vector3 InverseTransformPoint(Vector3 worldPos) => transform.InverseTransformPoint(worldPos);
        public Vector3 TransformPoint(Vector3 localPos) => transform.TransformPoint(localPos);
        public virtual void OnSetDirty(Type changedModuleType)
        {
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
