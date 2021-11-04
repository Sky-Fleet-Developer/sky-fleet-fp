using System;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
using Core.Utilities;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace Core.TerrainGenerator
{
    public class Deformer : MonoBehaviour, IDeformer
    {
        [ShowInInspector]
        public List<IDeformerLayerSetting> Settings
        {
            get
            {
                if (settings == null)
                {
                    ReadFromJson();
                }
                return settings;
            }
            set => settings = value;
        }

        public Rect AxisAlignedRect => axisAlignedRect;

        public Vector4 LocalRect => localRect;
        public float Fade => fade;


        [SerializeField]
        private Vector4 localRect;
        
        [SerializeField, Range(0f, 1f)]
        private float fade = 0.2f;

        private List<IDeformerLayerSetting> settings;

        [ShowInInspector] private Rect axisAlignedRect;
        [ShowInInspector] private List<Vector2Int> affectedChunks;

        [SerializeField, HideInInspector]
        private string[] typesInfo;

        [SerializeField, HideInInspector]
        private string[] jsonConfig;

        private void Start()
        {
            CalculateAxisAlignedRect();
        }

        [Button]
        public void AddDeformerSettings(System.Type deformer)
        {
            IDeformerLayerSetting layer = System.Activator.CreateInstance(deformer) as IDeformerLayerSetting;
            layer.Init(this);
            Settings.Add(layer);
        }

        [Button]
        public void SaveToJson()
        {
            if (settings == null) return;
            
            typesInfo = new string[settings.Count];
            jsonConfig = new string[settings.Count];
            for (int i = 0; i < settings.Count; i++)
            {
                typesInfo[i] = settings[i].GetType().FullName;
                jsonConfig[i] = JsonConvert.SerializeObject(settings[i]);
            }
        }

        [Button]
        public void ReadFromJson()
        {
            settings = new List<IDeformerLayerSetting>();
            if(jsonConfig == null || jsonConfig.Length == 0) return;
            
            for(int i = 0; i < jsonConfig.Length; i++)
            {
                System.Type type = TypeExtensions.GetTypeByName(typesInfo[i]);
                IDeformerLayerSetting newDeformer = JsonConvert.DeserializeObject(jsonConfig[i], type) as IDeformerLayerSetting;
                newDeformer.Init(this);
                settings.Add(newDeformer);
            }
        }


        private void CalculateAxisAlignedRect()
        {
            var rotation = transform.rotation;
            var position = transform.position;
            Vector3 leftDown = rotation * new Vector3(localRect.x - localRect.z * 0.5f, 0, localRect.y - localRect.w * 0.5f) + position;
            Vector3 leftUp = rotation * new Vector3(localRect.x - localRect.z * 0.5f, 0, localRect.y + localRect.w * 0.5f) + position;
            Vector3 rightDown = rotation * new Vector3(localRect.x + localRect.z * 0.5f, 0, localRect.y - localRect.w * 0.5f) + position;
            Vector3 rightUp = rotation * new Vector3(localRect.x + localRect.z * 0.5f, 0, localRect.y + localRect.w * 0.5f) + position;
            Vector2 min = new Vector2(Mathf.Min(leftDown.x, leftUp.x, rightDown.x, rightUp.x), Mathf.Min(leftDown.z, leftUp.z, rightDown.z, rightUp.z));
            Vector2 max = new Vector2(Mathf.Max(leftDown.x, leftUp.x, rightDown.x, rightUp.x), Mathf.Max(leftDown.z, leftUp.z, rightDown.z, rightUp.z));
            axisAlignedRect.min = min;
            axisAlignedRect.max = max;
        }

        public IEnumerable<Vector2Int> GetAffectChunks(float chunkSize)
        {
            if (affectedChunks == null) return affectedChunks;
            
            Vector4 rect = LocalRect;
            Vector3[] array =
            {
                transform.right * -rect.z * 0.5f + transform.forward * rect.w * 0.5f + transform.position,
                transform.right * rect.z * 0.5f + transform.forward * rect.w * 0.5f + transform.position,
                transform.right * rect.z * 0.5f + transform.forward * rect.w * 0.5f + transform.position,
                transform.right * -rect.z * 0.5f + transform.forward * -rect.w * 0.5f + transform.position,
            };

            affectedChunks = new List<Vector2Int>();
            foreach (Vector3 point in array)
            {
                Vector2Int newItem = 
                    new Vector2Int(Mathf.FloorToInt(point.x / chunkSize), Mathf.FloorToInt(point.y / chunkSize));
                if(affectedChunks.Contains(newItem) == false) affectedChunks.Add(newItem);
            }

            return affectedChunks;
        }
        
        public Terrain[] GetTerrainsContacts()
        {
            Vector4 rect = LocalRect;
            Ray[] rays = new Ray[4];
            Vector3 up = Vector3.up * 6000;
            rays[0] = new Ray(up + transform.right * -rect.z * 0.5f + transform.forward * rect.w * 0.5f + transform.position, Vector3.down);
            rays[1] = new Ray(up + transform.right * rect.z * 0.5f + transform.forward * rect.w * 0.5f + transform.position, Vector3.down);
            rays[2] = new Ray(up + transform.right * rect.z * 0.5f + transform.forward * -rect.w * 0.5f + transform.position, Vector3.down);
            rays[3] = new Ray(up + transform.right * -rect.z * 0.5f + transform.forward * -rect.w * 0.5f + transform.position, Vector3.down);
            List<Terrain> terrains = new List<Terrain>();
            for (int i = 0; i < rays.Length; i++)
            {
                RaycastHit[] hits = Physics.RaycastAll(rays[i], Mathf.Infinity);
                foreach (RaycastHit hit in hits)
                {
                    Terrain tr = hit.collider.gameObject.GetComponent<Terrain>();
                    if (tr != null)
                    {
                        if (terrains.Find(
                            new System.Predicate<Terrain>(x => { return x.terrainData == tr.terrainData; })) == null)
                        {
                            terrains.Add(tr);
                        }
                    }
                }
            }

            return terrains.ToArray();
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
        
        public static Rect GetAffectRectangle(Terrain terrain, Rect rect)
        {
            Vector3 pos = terrain.transform.position;
            rect.position -= pos.XZ();

            float terrainSizeInv = 1f / terrain.terrainData.size.x;

            rect.position *= terrainSizeInv;
            rect.size *= terrainSizeInv;

            return rect;
        }
        public static Rect GetAffectRectangle(TerrainData data, Vector3 terrainPosition, Rect rect)
        {
            rect.position -= terrainPosition.XZ();

            float terrainSizeInv = 1f / data.size.x;

            rect.position *= terrainSizeInv;
            rect.size *= terrainSizeInv;

            return rect;
        }
    }
}