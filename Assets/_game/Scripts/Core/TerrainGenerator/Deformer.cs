using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
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
            set
            {
                settings = value;
            }
        }

        public Rect AxisAlinedRect
        {
            get
            {
                return axisAlinedRect;
            }
        }
        public Rect LocalAlinedRect { get { return localAlined; } }

        [SerializeField]
        private Rect localAlined;


        private List<IDeformerLayerSetting> settings;

        private Rect axisAlinedRect;

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
            settings.Add(layer);
        }

        [Button]
        public void SaveToJson()
        {
             
            if (settings != null)
            {
                typesInfo = new string[settings.Count];
                jsonConfig = new string[settings.Count];
                for (int i = 0; i < settings.Count; i++)
                {
                    typesInfo[i] = settings[i].GetType().FullName;
                    jsonConfig[i] = JsonConvert.SerializeObject(settings[i]);
                }
                
            }
        }

        [Button]
        public void ReadFromJson()
        {
            if(jsonConfig == null)
            {
                settings = new List<IDeformerLayerSetting>();
                return;
            }
            for(int i = 0; i < jsonConfig.Length; i++)
            {
                //System.Type type = 
            }
        }


        private void CalculateAxisAlinedRect()
        {
            Vector3 leftDown = transform.rotation * new Vector3(localAlined.position.x, 0, localAlined.position.y) + transform.position;
            Vector3 leftUp = transform.rotation * new Vector3(localAlined.position.x, 0, localAlined.position.y + localAlined.size.y) + transform.position;
            Vector3 rightDown = transform.rotation * new Vector3(localAlined.position.x + localAlined.size.x, 0, localAlined.position.y) + transform.position;
            Vector3 rightUp = transform.rotation * new Vector3(localAlined.position.x + localAlined.size.x, 0, localAlined.position.y + localAlined.size.y) + transform.position;
            axisAlinedRect = new Rect(Mathf.Min(leftDown.x, leftUp.x, rightDown.x, rightUp.x), Mathf.Min(leftDown.y, leftUp.y, rightDown.y, rightUp.y),
                Mathf.Max(leftDown.x, leftUp.x, rightDown.x, rightUp.x), Mathf.Max(leftDown.y, leftUp.y, rightDown.y, rightUp.y));
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(localAlined.width, 0, localAlined.height));
        }
    }
}