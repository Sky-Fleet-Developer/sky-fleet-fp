using UnityEngine;

namespace Core.View
{
    [CreateAssetMenu(menuName = "Game/ViewSettings", fileName = "ViewSettings")]
    public class ViewSettings : ScriptableObject
    {
        public float viewRadius;
    }
}