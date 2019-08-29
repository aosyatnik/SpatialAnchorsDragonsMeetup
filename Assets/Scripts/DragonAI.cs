using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonAI : MonoBehaviour
{
    public DragonModel Dragon { get; private set; }

    public DragonAI()
    {
        Dragon = new DragonModel();
    }

    void Start()
    {
        var initialTransform = GetComponent<MeshRenderer>().transform;
        Dragon.SetInitTransform(initialTransform);
    }

    void Update()
    {
#if UNITY_WSA || WINDOWS_UWP
        // Main camera position is player's position.
        Dragon.TrackPlayerPosition(Camera.main.transform.position);
#endif
    }
}
