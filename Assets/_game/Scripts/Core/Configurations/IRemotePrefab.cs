using System.Collections.Generic;
using UnityEngine;

namespace Core.Configurations
{
    public interface IRemotePrefab
    {
        // ReSharper disable once InconsistentNaming
        Transform transform { get; }
        string AssetId { get; }
        List<string> Tags { get; }
    }
}