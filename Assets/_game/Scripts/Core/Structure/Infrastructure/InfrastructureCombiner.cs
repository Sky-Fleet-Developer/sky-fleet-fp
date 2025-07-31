using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using UnityEngine;

public class InfrastructureCombiner : MonoBehaviour, ILoadAtStart
{
    public Task Load()
    {
        
        return Task.CompletedTask;
    }
}
