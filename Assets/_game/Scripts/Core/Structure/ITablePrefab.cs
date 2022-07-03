using System.Collections.Generic;
using UnityEngine;

namespace Core.Structure
{
    public interface ITablePrefab
    {
        Transform transform { get; }
        string Guid { get; }
        List<string> Tags { get; }
    }
}