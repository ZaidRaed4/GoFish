using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class BackButton : MonoBehaviour
{
    static readonly List<Action> Open = new();

    public static void Push(Action close)
    {
        Open.Remove(close);
        Open.Add(close);
    }

    public static void Remove(Action close) => Open.Remove(close);

    [Tooltip("Runs when back is pressed and no screen is open")]
    [SerializeField] UnityEvent fallback = new();

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (Open.Count > 0) Open[Open.Count - 1]();
        else fallback.Invoke();
    }
}
