using System.Collections;
using System.Collections.Generic;
using Structure.Rigging;
using UnityEngine;

namespace Character.Control
{
    public interface ICharacterController
    {
        IControl AttachedControl { get; }
        IEnumerator AttachToControl(IControl control);
        IEnumerator LeaveControl(CharacterDetachhData detachData);
    }
}
