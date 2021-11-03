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

        [SerializeField, HideInInspector]
        private string[] typesInfo;

        [SerializeField, HideInInspector]
        private string[] jsonConfig;

        private void Start()
        {
            CalculateAxisAlinedRect();
        }

        [Button]
        public void AddDeformerSettings(System.Type deformer)
        {
            IDeformerLayerSetting layer = System.Activator.CreateInstance(deformer) as IDeformerLayerSetting;
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


        private void CalculateAxisAlinedRect()
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

        private void OnDrawGizmosSelected()
        {
            CalculateAxisAlinedRect();

            Gizmos.DrawWireCube(new Vector3(axisAlignedRect.position.x + axisAlignedRect.width * 0.5f, transform.position.y, axisAlignedRect.position.y + axisAlignedRect.height * 0.5f), new Vector3(axisAlignedRect.width, 0, axisAlignedRect.height));

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.white * 0.8f;
            Gizmos.DrawWireCube(new Vector3(localRect.x, 0, localRect.y), new Vector3(localRect.z, 0, localRect.w));
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(new Vector3(localRect.x, 0, localRect.y), new Vector3(localRect.z, 0, localRect.w) * (1 - fade));
        }
    }
}