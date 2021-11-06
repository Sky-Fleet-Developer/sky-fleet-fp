using Core.Boot_strapper;
using Core.Utilities;
using Runtime.Character.Control;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Runtime.Character
{
    public class SpawnPerson : Singleton<SpawnPerson>, ILoadAtStart
    {
        public FirstPersonController Player { get => player; }

        [SerializeField] private FirstPersonController player;



        public Task LoadStart()
        {
            player.gameObject.SetActive(true);
            return Task.CompletedTask;
        }
    }
}