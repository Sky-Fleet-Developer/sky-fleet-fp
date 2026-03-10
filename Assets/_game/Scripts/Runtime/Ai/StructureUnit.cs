using AYellowpaper;
using Core.Ai;
using UnityEngine;

namespace Runtime.Ai
{
    public class StructureUnit : MonoBehaviour, IUnit
    {
        [SerializeField, SerializeReference] private IUnitTactic myTactic;

        public void SetTactic(IUnitTactic tactic)
        {
            myTactic = tactic;
        }
    }
}