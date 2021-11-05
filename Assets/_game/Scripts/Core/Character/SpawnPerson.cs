using Core.Boot_strapper;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Runtime.Character
{
    public class SpawnPerson : Singleton<SpawnPerson>, ILoadAtStart
    {
        [SerializeField] private GameObject player;

        public Task LoadStart()
        {
            player.SetActive(true);
            return Task.CompletedTask;
        }
    }
}