using System.Collections;
using System.Collections.Generic;
using Structure.Rigging;
using UnityEngine;

namespace Character.Control
{
    public interface ICharacterController
    {
        IEnumerator AttachToControl(IControl control);
    }
}
