using System;
using Core.Boot_strapper;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cinemachine;
using Core;
using Core.Character;
using UnityEngine;

namespace Runtime.Character
{
    public class SpawnPerson : Singleton<SpawnPerson>, ILoadAtStart
    {
        public static LateEvent OnPlayerWasLoaded = new LateEvent();

        public FirstPersonController Player => player;

        private FirstPersonController player;
        [SerializeField] private FirstPersonController source;

        private void Start()
        {
            CinemachineVirtualCamera camHolder = GetComponentInChildren<CinemachineVirtualCamera>();
            if (camHolder) camHolder.enabled = false;
        }

        public Task Load()
        {
            Vector3 spawnPos = transform.position;
            if (Physics.Raycast(transform.position + Vector3.up * 10000, Vector3.down, out RaycastHit groundHit, 11000, GameData.Data.walkableLayer))
            {
                spawnPos = groundHit.point + Vector3.up;
            }
            player = Instantiate(source, spawnPos, transform.rotation);
            player.gameObject.SetActive(false);
            OnPlayerWasLoaded.Invoke();
            Bootstrapper.OnLoadComplete.Subscribe(() =>
            {
                player.gameObject.SetActive(true);
            });
            return Task.CompletedTask;
        }
    }
}