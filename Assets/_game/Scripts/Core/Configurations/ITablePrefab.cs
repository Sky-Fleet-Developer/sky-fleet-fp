using System.Collections.Generic;
using UnityEngine;

namespace Core.Configurations
{
    public interface ITablePrefab
    {
        // ReSharper disable once InconsistentNaming
        Transform transform { get; }
        string Guid { get; }
        List<string> Tags { get; }
    }
}