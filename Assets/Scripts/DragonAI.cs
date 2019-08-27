using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonAI : MonoBehaviour
{
    public DragonModel Dragon { get; private set; }

    void Start()
    {
        var initialPosition = gameObject.GetComponentInChildren<MeshRenderer>().transform.position;
        Dragon = new DragonModel(initialPosition);
        Dragon.OnPositionChanged += Dragon_OnPositionChanged;
    }

    private void Dragon_OnPositionChanged(Vector3 newPosition)
    {
        gameObject.transform.position = newPosition;
    }

    void Update()
    {
#if UNITY_WSA || WINDOWS_UWP
        // Main camera position is player's position.
        Dragon.TrackPlayerPosition(Camera.main.transform.position);
#endif
    }
}
