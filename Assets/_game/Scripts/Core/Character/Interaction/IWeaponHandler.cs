using System.Collections.Generic;
using UnityEngine;

namespace Core.Character.Interaction
{
    public interface IWeaponHandler : ICharacterHandler
    {
        public bool CanAimHorizontally {get;}
        public bool CanAimVertically {get;}
        public float HorizontalAimAxis { get; set; }
        public float VerticalAimAxis { get; set; }
        public void Fire();
        public void ResetControls();

    }
}