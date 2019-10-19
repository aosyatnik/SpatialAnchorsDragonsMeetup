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
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    CreateAnchorAsync(touch.position);
                }
            }
        }
    }

    private async Task CreateAnchorAsync(Vector2 position)
    {
        // Create a local anchor, perhaps by hit-testing and spawning an object within the scene
        Vector3 hitPosition = GetHitPosition_Android(position);

        Quaternion rotation = Quaternion.AngleAxis(0, Vector3.up);
        InstantiateLocalGameObject(hitPosition, rotation);
        localAnchorGameObject.AddARAnchor();

        CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
        cloudAnchor.LocalAnchor = localAnchorGameObject.GetNativeAnchorPointer();
        cloudAnchor.AppProperties[@"label"] = @"Dragon";

        await cloudSession.CreateAnchorAsync(cloudAnchor);
        await storageService.PostAnchorId(cloudAnchor.Identifier);
        Debug.Log(DEBUG_FILTER + $"Created a cloud anchor with ID={cloudAnchor.Identifier}");
    }

    public Vector3 GetHitPosition_Android(Vector2 position)
    {
        TrackableHit hit;
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon | TrackableHitFlags.FeaturePointWithSurfaceNormal;
        Debug.Log(DEBUG_FILTER + $"Creating raycast.");
        if (Frame.Raycast(position.x, position.y, raycastFilter, out hit))
        {
            Debug.Log(DEBUG_FILTER + $"{hit.Pose.position}");
            return hit.Pose.position;
        }
        return new Vector3();
    }
}
