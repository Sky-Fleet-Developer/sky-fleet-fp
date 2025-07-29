using System.Collections.Generic;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class AltitudeIndicator : MonoBehaviour, IDevice
    {
        [ShowInInspector]
        public string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(guid))
                {
                    guid = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
                 UnityEditor.EditorUtility.SetDirty(this);   
#endif
                }
                return guid;
            }
            set => guid = value;
        }
        [SerializeField, HideInInspector] private string guid;

        public List<string> Tags => tags;
        [SerializeField] private List<string> tags;

        public IGraphHandler Graph => _graph;
        public IBlock Block => _block;

        [SerializeField] private Transform bubble_pitch;
        [SerializeField] private Transform bubble_roll;
        [SerializeField] private Transform compass;

        private IBlock _block;
        private IGraphHandler _graph;


        public void Init(IGraphHandler graph, IBlock block)
        {
            _block = block;
            _graph = graph;
        }

        public void UpdateDevice()
        {
            float pitch = -Mathf.Asin(transform.forward.y) * Mathf.Rad2Deg;
            float roll = -Mathf.Atan2(transform.right.y, transform.up.y) * Mathf.Rad2Deg;
            bubble_pitch.localRotation = Quaternion.Euler(pitch, 0, 0);
            bubble_roll.localRotation = Quaternion.Euler(0, 0, roll);
            compass.localEulerAngles = new Vector3(0, -transform.eulerAngles.y, 0);
        }

        public Port GetPort() => null;

        public string GetName() => null;
    }
}