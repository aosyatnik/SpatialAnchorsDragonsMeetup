using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugWindow : MonoBehaviour
{
    TextMesh _textMesh;

    // Use this for initialization
    void Start()
    {
        _textMesh = gameObject.GetComponentInChildren<TextMesh>();
    }

    void OnEnable()
    {
        Application.logMessageReceived += LogMessage;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= LogMessage;
    }

    public void LogMessage(string message, string stackTrace, LogType type)
    {
        if (_textMesh.text.Length > 300)
        {
            _textMesh.text = message + "\n";
        }
        else
        {
            _textMesh.text += message + "\n";
        }
    }
}
