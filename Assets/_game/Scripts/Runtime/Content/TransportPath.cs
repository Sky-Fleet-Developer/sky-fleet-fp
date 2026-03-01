using System;
using Core.Items;
using Runtime.Misc;
using UnityEngine;

namespace Runtime.Content
{
    [RequireComponent(typeof(SpsViewRange))]
    public class TransportPath : MonoBehaviour
    {
        [SerializeField] private EntityObjectInstaller entityToSpawn;
        private SpsViewRange _viewRangeSpline;

        private void Awake()
        {
            _viewRangeSpline = GetComponent<SpsViewRange>();
            
        }
    }
}