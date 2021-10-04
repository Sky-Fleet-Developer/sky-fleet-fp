using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Core.Menu
{
    public interface ILoadAtStart
    {
        Task Load();


    }
}