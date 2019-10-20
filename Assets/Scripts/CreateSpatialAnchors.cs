using GoogleARCore;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors.Unity.Android;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CreateSpatialAnchors : AbstractSpatialAnchor
{

    // Update is called once per frame
    protected override void Update()
    {
        ProcessLatestFrame();

        if (ScannedPercent > 1.0f)
        {
            // If the player has not touched the screen, we are done with this update.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            CreateAnchorAsync(touch.position);
        }
    }

    private async Task CreateAnchorAsync(Vector2 position)
    {
        // Create a local anchor, perhaps by hit-testing and spawning an object within the scene

        TrackableHit hit;
        Vector3 hitPosition;
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon | TrackableHitFlags.FeaturePointWithSurfaceNormal;
        Debug.Log(DEBUG_FILTER + $"Creating raycast. Click position:{position}");
        if (Frame.Raycast(position.x, position.y, raycastFilter, out hit))
        {
            Debug.Log(DEBUG_FILTER + $"{hit.Pose.position}");
            hitPosition = hit.Pose.position;
        }
        else
        {
            // Couldn't create raycast.
            return;
        }

        Quaternion rotation = Quaternion.AngleAxis(0, Vector3.up);
        Debug.Log($"ASA CreateAnchorAsync: {hitPosition}");
        InstantiateLocalGameObject(hitPosition, rotation);
        localAnchorGameObject.AddARAnchor();

        CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
        cloudAnchor.LocalAnchor = localAnchorGameObject.GetNativeAnchorPointer();
        cloudAnchor.AppProperties[@"label"] = @"Dragon";

        await cloudSession.CreateAnchorAsync(cloudAnchor);
        await storageService.PostAnchorId(cloudAnchor.Identifier);
        Debug.Log(DEBUG_FILTER + $"Created a cloud anchor with ID={cloudAnchor.Identifier}");
    }
}
