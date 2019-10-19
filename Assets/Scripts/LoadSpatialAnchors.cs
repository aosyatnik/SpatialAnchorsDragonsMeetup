// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS
using Microsoft.Azure.SpatialAnchors.Unity.IOS.ARKit;
using UnityEngine.XR.iOS;
#elif UNITY_ANDROID
using GoogleARCore;
using Microsoft.Azure.SpatialAnchors.Unity.Android;
using Microsoft.Azure.SpatialAnchors.Unity.Android.ARCore;
#elif UNITY_WSA || WINDOWS_UWP
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Input;
#endif

namespace Microsoft.Azure.SpatialAnchors.Unity
{
    /// <summary>
    /// Use this behavior to manage an Azure Spatial Service session for your game or app.
    /// </summary>
    public class LoadSpatialAnchors : AbstractSpatialAnchor
    {

        #region private members

        /// <summary>
        /// When next time anchor will be updated.
        /// </summary>
        private int nextUpdate = 0;

        /// <summary>
        /// 1 second, anchor will be updated once per second.
        /// </summary>
        private int duration = 1;

        private string currentlyLoadingAnchorId = "";
        #endregion

        protected override void Update()
        {
            ProcessQueue();
            ProcessLatestFrame();

            if (Time.time < nextUpdate)
            {
                return;
            }
            nextUpdate = Mathf.FloorToInt(Time.time) + duration;

            if (ScannedPercent > 1.0f)
            {
                LoadAnchorAsync();
            }
        }

        private async void LoadAnchorAsync()
        {
            AnchorLocateCriteria criteria = new AnchorLocateCriteria();
            string anchorId = await storageService.GetAnchorId();

            // Local anchor is the same as the last saved one. No need to reload it.
            if (currentlyLoadingAnchorId == anchorId || loadedAnchor != null && loadedAnchor.Identifier == anchorId)
            {
                return;
            }

            Debug.Log(DEBUG_FILTER + "we are going to load anchor with id: " + anchorId);
            currentlyLoadingAnchorId = anchorId;

            // Stop all other watchers.
            foreach (var w in cloudSession.GetActiveWatchers())
            {
                w.Stop();
            }

            criteria.Identifiers = new string[] { anchorId };
            cloudSession.CreateWatcher(criteria);

            cloudSession.AnchorLocated += (object sender, AnchorLocatedEventArgs args) =>
            {
                switch (args.Status)
                {
                    case LocateAnchorStatus.Located:
                        loadedAnchor = args.Anchor;
                        // Run on UI thread.
                        QueueOnUpdate(() => SpawnOrMoveCurrentAnchoredObject(loadedAnchor));
                        break;
                    case LocateAnchorStatus.AlreadyTracked:
                        Debug.Log(DEBUG_FILTER + "Anchor already tracked. Identifier: " + args.Identifier);
                        loadedAnchor = args.Anchor;
                        break;
                    case LocateAnchorStatus.NotLocatedAnchorDoesNotExist:
                        Debug.Log(DEBUG_FILTER + "Anchor not located. Identifier: " + args.Identifier);
                        break;
                    case LocateAnchorStatus.NotLocated:
                        Debug.LogError("ASA Error: Anchor not located does not exist. Identifier: " + args.Identifier);
                        break;
                }
            };
        }

        private void SpawnOrMoveCurrentAnchoredObject(CloudSpatialAnchor foundAnchor)
        {
            if (localAnchorGameObject is null)
            {
                InstantiateLocalGameObject(Pose.identity.position, Pose.identity.rotation);
            }

            // Get anchor position.
            Pose anchorPose = foundAnchor.GetAnchorPose();
            Debug.Log("ASA log: new position:" + anchorPose.position);

            localAnchorGameObject.transform.position = anchorPose.position;
        }
    }
}
