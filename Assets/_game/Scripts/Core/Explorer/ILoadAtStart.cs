using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Core.Boot_strapper
{
    public interface ILoadAtStart
    {
        Task Load();


    }
}