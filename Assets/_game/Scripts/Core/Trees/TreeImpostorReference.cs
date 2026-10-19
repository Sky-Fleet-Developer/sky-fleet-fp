using UnityEngine;

namespace Core.Trees
{
    /// <summary>On a tree prefab: the impostor made from it.</summary>
    public class TreeImpostorReference : MonoBehaviour
    {
        [SerializeField] private TreeImpostor impostor;

        public TreeImpostor Impostor
        {
            get => impostor;
            set => impostor = value;
        }
    }
}
