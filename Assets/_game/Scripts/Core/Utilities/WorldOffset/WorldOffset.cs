using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Core.Boot_strapper;
using System.Threading.Tasks;
using Runtime.Character.Control;
using Core.SessionManager;
using System;
using Sirenix.OdinInspector;

namespace Core.Utilities
{
    public class WorldOffset : Singleton<WorldOffset>, ILoadAtStart
    {
        [ShowInInspector]
        public Vector3 Offset { get; set; }

        public event Action<Vector3> SendWorldMove;

        [SerializeField] private Vector3 limit;

        [SerializeField] private Transform viewer; 

        public Task LoadStart()
        {
            return Task.CompletedTask;
        }

        private void Update()
        {

            Vector3 playerPos = viewer.position;
            Vector3 newPos = Vector3.zero;
            if (playerPos.x > 0 && limit.x < playerPos.x)
            {
                Offset = new Vector3(Offset.x - limit.x, 0, Offset.z);
                newPos += new Vector3(-limit.x, 0, 0);
            }
            else if (playerPos.x < 0 && limit.x < -playerPos.x)
            {
                Offset = new Vector3(Offset.x + limit.x, 0, Offset.z);
                newPos += new Vector3(limit.x, 0, 0);
            }

            if (playerPos.z > 0 && limit.z < playerPos.z)
            {
                Offset = new Vector3(Offset.x, 0, Offset.z - limit.z);
                newPos += new Vector3(0, 0, -limit.z);
            }
            else if (playerPos.z < 0 && limit.z < -playerPos.z)
            {
                Offset = new Vector3(Offset.x, 0, Offset.z + limit.z);
                newPos += new Vector3(0, 0, limit.z);
            }

            //Debug.Log(newPos);
            //SendWorldMove?.Invoke(newPos);
        }
    }
}