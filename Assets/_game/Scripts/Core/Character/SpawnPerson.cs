using Core.Boot_strapper;
using Core.Utilities;
using Runtime.Character.Control;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using UnityEngine;

namespace Runtime.Character
{
    public class SpawnPerson : Singleton<SpawnPerson>, ILoadAtStart
    {
        public static LateEvent OnPlayerWasLoaded = new LateEvent();

        public FirstPersonController Player => player;

        private FirstPersonController player;
        [SerializeField] private FirstPersonController source;
        

        public Task Load()
        {
            Vector3 spawnPos = transform.position;
            if (Physics.Raycast(transform.position + Vector3.up * 10000, Vector3.down, out RaycastHit groundHit, 11000, GameData.Data.groundLayer))
            {
                spawnPos = groundHit.point + Vector3.up;
            }
            player = Instantiate(source, spawnPos, transform.rotation);
            player.gameObject.SetActive(false);
            Bootstrapper.OnLoadComplete.Subscribe(() =>
            {
                player.gameObject.SetActive(true);
                OnPlayerWasLoaded.Invoke();
            });
            return Task.CompletedTask;
        }
    }
}