using Microsoft.Azure.SpatialAnchors.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonAI : MonoBehaviour
{
    private bool positionTracking = true;

    public DragonModel Dragon { get; private set; }

    public DragonAI()
    {
        Dragon = new DragonModel();
    }

    void Start()
    {
        var initialTransform = GetComponent<MeshRenderer>().transform;
        Dragon.SetInitPosition(initialTransform.position);
    }

    void Update()
    {
#if UNITY_WSA || WINDOWS_UWP
        // Main camera position is player's position.
        if(positionTracking)
        {
            Dragon.TrackPlayerPosition(Camera.main.transform.position);
        }
#endif
    }

    public void StopPlayerPositionTracking()
    {
        positionTracking = false;
    }

    public void StartPlayerPositionTracking()
    {
        positionTracking = true;
    }
}
