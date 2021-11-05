using Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{

    private KeysControl.Request pressB;

    void Start()
    {
        pressB = KeysControl.Instance.RegisterRequest("Move player", "Move forward", KeysControl.PressType.Down, Press);
    }

    private void Press()
    {
        KeysControl.Instance.RemoveReques(pressB);
        Debug.Log("press");
    }
}
