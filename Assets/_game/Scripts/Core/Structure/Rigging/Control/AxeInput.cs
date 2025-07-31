using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Structure.Rigging.Control
{
    [System.Serializable]
    public class AxeInput
    {
        [SerializeField]
        private string nameAxe;

        public string GetNameAxe() => nameAxe;

        [Button]
        public void SetAxe(string name)
        {
            nameAxe = name;
        }

        public float GetValue()
        {
            return Input.GetAxisRaw(nameAxe);
        }

        public bool IsNone()
        {
            return string.IsNullOrEmpty(nameAxe);
        }
    }
}