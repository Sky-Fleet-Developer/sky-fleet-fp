using Core.Character.Interface;
using Core.Patterns.State;
using Core.UiStructure;
using UnityEngine;

namespace Runtime.Trading.UI
{
    public class PlayerInventoryInterface : FirstPersonService
    {

        public override bool IsMatch(IState state)
        {
            return true;
        }
    }
}