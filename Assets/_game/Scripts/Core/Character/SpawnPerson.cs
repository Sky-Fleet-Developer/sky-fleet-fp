using System;
using Core.Boot_strapper;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Character;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Runtime.Character
{
    public class SpawnPerson : Singleton<SpawnPerson>, ILoadAtStart
    {
        [SerializeField] private int rulesSurveyFrequency;
        public static LateEvent OnPlayerWasLoaded = new LateEvent();

        public FirstPersonController Player => player;

        private FirstPersonController player;
        [SerializeField] private FirstPersonController source;
        bool ILoadAtStart.enabled => enabled && gameObject.activeInHierarchy;
        [Inject] private DiContainer _diContainer;
        private void Start()
        {
            CinemachineVirtualCamera camHolder = GetComponentInChildren<CinemachineVirtualCamera>();
            if (camHolder) camHolder.enabled = false;
        }

        public async Task Load()
        {
            while (Application.isPlaying)
            {
                PersonSpawnRule[] rules = GetComponentsInChildren<PersonSpawnRule>();
                foreach (var rule in rules)
                {
                    if (!rule.TryGetSpawnPoint(out var spawnPosition)) continue;
                    
                    player = Instantiate(source, spawnPosition, transform.rotation);
                    _diContainer.InjectGameObject(player.gameObject);
                    player.gameObject.SetActive(false);
                    OnPlayerWasLoaded.Invoke();
                    Bootstrapper.OnLoadComplete.Subscribe(() =>
                    {
                        player.gameObject.SetActive(true);
                    });
                    return;
                }
                await Task.Delay((int)(1000f / rulesSurveyFrequency));
            }
        }

        private void OnDestroy()
        {
            OnPlayerWasLoaded.Reset();
        }
    }
}