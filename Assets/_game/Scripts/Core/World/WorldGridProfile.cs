using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.World
{
    [CreateAssetMenu(menuName = "Game/WorldObjectsOcclusionProfile")]
    public class WorldGridProfile : ScriptableObject
    {
        [InlineProperty] public WorldGridData data;
    }
}