using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.World;
using UnityEngine;

namespace Runtime.EventBehaviours
{
    public class SpawnerBehaviour : MonoBehaviour, ITablePrefab
    {
        public string Guid { get; }
        public List<string> Tags { get; }
    }
}