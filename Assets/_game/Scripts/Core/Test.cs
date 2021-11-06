using Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private KeysControl.KeyToRequest pressA;
    private KeysControl.KeyToRequest pressB;

    void Start()
    {
        pressA = KeysControl.Instance.RegisterRequest("Move player", "Move forward", KeysControl.PressType.Down, Press);
        pressB = KeysControl.Instance.RegisterRequest("Move player", "Move forward", KeysControl.PressType.Down, Press2);
    }

    private void Press()
    {
        KeysControl.Instance.RemoveRequest(pressA);
        Debug.Log("press");
    }

    private void Press2()
    {
        Debug.Log("press2");
    }
}
