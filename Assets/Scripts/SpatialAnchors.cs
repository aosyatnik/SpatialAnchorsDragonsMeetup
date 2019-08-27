// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
#if UNITY_IOS
using Microsoft.Azure.SpatialAnchors.Unity.IOS.ARKit;
using UnityEngine.XR.iOS;
#elif UNITY_ANDROID
using GoogleARCore;
using Microsoft.Azure.SpatialAnchors.Unity.Android;
using Microsoft.Azure.SpatialAnchors.Unity.Android.ARCore;
#elif UNITY_WSA || WINDOWS_UWP
using UnityEngine.XR.WSA;
#endif

namespace Microsoft.Azure.SpatialAnchors.Unity
{


    /// <summary>
    /// Use this behavior to manage an Azure Spatial Service session for your game or app.
    /// </summary>
    public class SpatialAnchors : MonoBehaviour
    {
        // TODO: remove me
        public static string DEBUG_FILTER = "ASA Log:";
        public static bool crealedOrLoaded = false;

        private CloudSpatialAnchorSession cloudSession = null;
        private GameObject localAnchor;
        public float ScannedPercent;

        public GameObject AnchoredObjectPrefab = null;

        private void Start()
        {
#if UNITY_ANDROID
            Start_Android();
#else
            CreateNewCloudSession();
#endif
        }

        /// <summary>
        /// Our queue of actions that will be executed on the main thread.
        /// </summary>
        private readonly Queue<Action> dispatchQueue = new Queue<Action>();

        /// <summary>
        /// Queues the specified <see cref="Action"/> on update.
        /// </summary>
        /// <param name="updateAction">The update action.</param>
        protected void QueueOnUpdate(Action updateAction)
        {
            lock (dispatchQueue)
            {
                dispatchQueue.Enqueue(updateAction);
            }
        }

        private void ProcessQueue()
        {
            lock (dispatchQueue)
            {
                if (dispatchQueue.Count > 0)
                {
                    dispatchQueue.Dequeue()();
                }
            }
        }

        void Update()
        {
#if UNITY_WSA || WINDOWS_UWP
            ProcessQueue();
#endif
#if UNITY_ANDROID
            ProcessLatestFrame();
#endif
            if (!crealedOrLoaded && ScannedPercent > 1.0f)
            {
                //CreateAnchorAsync();
                LoadAnchorAsync();
                crealedOrLoaded = true;
            }
        }

        private void CreateNewCloudSession()
        {
            cloudSession = new CloudSpatialAnchorSession();
            cloudSession.Configuration.AccountId = @"2e18c3fe-5f34-493c-8ce0-a1216bcfe882";
            cloudSession.Configuration.AccountKey = @"bAzanG4bRXyPWqkwYT9mAwxqpy1m8h6I7hLf99m+Ze0=";
#if UNITY_IOS
            cloudSpatialAnchorSession.Session = arkitSession.GetNativeSessionPtr();
#elif UNITY_ANDROID
            cloudSession.Session = GoogleARCoreInternal.ARCoreAndroidLifecycleManager.Instance.NativeSession.SessionHandle;
#elif UNITY_WSA || WINDOWS_UWP
            // No need to set a native session pointer for HoloLens.
#else
            throw new NotSupportedException("The platform is not supported.");
#endif
            cloudSession.LogLevel = SessionLogLevel.All;

            cloudSession.Error += CloudSession_Error;
            cloudSession.OnLogDebug += CloudSession_OnLogDebug;
            cloudSession.SessionUpdated += CloudSession_SessionUpdated;
            cloudSession.Start();

            Debug.Log(DEBUG_FILTER + "Session was initialized.");
        }

        private void CloudSession_OnLogDebug(object sender, OnLogDebugEventArgs args)
        {
            //Debug.Log("ASA Log: " + args.Message);
        }

        private void CloudSession_Error(object sender, SessionErrorEventArgs args)
        {
            Debug.LogError("ASA Error: " + args.ErrorMessage);
        }

        private void CloudSession_SessionUpdated(object sender, SessionUpdatedEventArgs args)
        {
            var status = args.Status;
            ScannedPercent = status.RecommendedForCreateProgress;
            if (ScannedPercent < 1)
            {
                Debug.Log(DEBUG_FILTER + "recommendedForCreate: " + args.Status.RecommendedForCreateProgress);
            }
        }


        private void LoadAnchorAsync()
        {
            Debug.Log(DEBUG_FILTER + "criteria creating");
            AnchorLocateCriteria criteria = new AnchorLocateCriteria();
            criteria.Identifiers = new string[] { @"d00b7874-3cef-40a6-aaba-cdbca8c86513" };
            cloudSession.CreateWatcher(criteria);
            Debug.Log(DEBUG_FILTER + "created watcher");

            cloudSession.AnchorLocated += (object sender, AnchorLocatedEventArgs args) =>
            {
                Debug.Log(DEBUG_FILTER + "something is loaded");
                switch (args.Status)
                {
                    case LocateAnchorStatus.Located:
                        CloudSpatialAnchor foundAnchor = args.Anchor;
#if UNITY_WSA || WINDOWS_UWP
                        // Run on UI thread.
                        QueueOnUpdate(() => SpawnOrMoveCurrentAnchoredObject(foundAnchor));
#else
                        SpawnOrMoveCurrentAnchoredObject(foundAnchor);
#endif
                        break;
                    case LocateAnchorStatus.AlreadyTracked:
                        Debug.Log(DEBUG_FILTER + "Anchor already tracked. Identifier: " + args.Identifier);
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

        private GameObject SpawnOrMoveCurrentAnchoredObject(CloudSpatialAnchor foundAnchor)
        {
            Pose anchorPose = Pose.identity;
#if UNITY_ANDROID || UNITY_IOS
            // Android and ios use GetAnchorPose for getting position.
            anchorPose = foundAnchor.GetAnchorPose();
#endif
            GameObject newGameObject = GameObject.Instantiate(AnchoredObjectPrefab, anchorPose.position, anchorPose.rotation);

            // Mark game object as anchor.
#if UNITY_ANDROID
            newGameObject.AddComponent<UnityARCoreWorldAnchorComponent>();
#elif UNITY_WSA || WINDOWS_UWP
            newGameObject.AddComponent<WorldAnchor>();
#endif

#if UNITY_WSA || WINDOWS_UWP
            // Hololens is using SetNativeSpatialAnchorPtr for getting position.
            // TODO: fix it! Loaded wrong data. WHY?! Dragon should be in front of user.
            // newGameObject.GetComponent<WorldAnchor>().SetNativeSpatialAnchorPtr(foundAnchor.LocalAnchor);
#endif
            Debug.Log(DEBUG_FILTER + $"Position: {newGameObject.transform.position} Rotation: {newGameObject.transform.rotation}");

            // Add ai.
            newGameObject.AddComponent<DragonAI>();
            //newGameObject.GetComponent<DragonAI>().Dragon.OnPositionChanged += Dragon_OnPositionChanged;

            // Animate
            newGameObject.AddComponent<MeshRenderer>();
            Animator animator = newGameObject.GetComponent<Animator>();
            var controller = Resources.Load("Dragon Animator Controller") as RuntimeAnimatorController;
            animator.runtimeAnimatorController = controller;
            newGameObject.AddComponent<DragonAnimation>();

            return newGameObject;
        }

        /// <summary>
        /// Save new position to anchors.
        /// </summary>
        private void Dragon_OnPositionChanged(Vector3 newPosition)
        {
            // TODO: implement this.
        }

        private async Task CreateAnchorAsync()
        {
            // Create a local anchor, perhaps by hit-testing and spawning an object within the scene
            Vector3 hitPosition = new Vector3();

#if UNITY_IOS
            hitPosition = GetHitPosition_IOS();
#elif UNITY_ANDROID
            hitPosition = GetHitPosition_Android();
#elif WINDOWS_UWP || UNITY_WSA
            hitPosition = GetHitPosition_Hololens();
#endif

            Quaternion rotation = Quaternion.AngleAxis(0, Vector3.up);
            localAnchor = GameObject.Instantiate(AnchoredObjectPrefab, hitPosition, rotation);
            localAnchor.AddARAnchor();

            // If the user is placing some application content in their environment,
            // you might show content at this anchor for a while, then save when
            // the user confirms placement.
            CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
            cloudAnchor.LocalAnchor = localAnchor.GetNativeAnchorPointer();
            cloudAnchor.AppProperties[@"label"] = @"Dragon";
            await cloudSession.CreateAnchorAsync(cloudAnchor);
            Debug.Log(DEBUG_FILTER + $"Created a cloud anchor with ID={cloudAnchor.Identifier}");
        }

        #region Android
#if UNITY_ANDROID
        /// <summary>
        /// We should only run the java initialization once.
        /// </summary>
        private static bool JavaInitialized { get; set; } = false;

        /// <summary>
        /// Init java, only for android.
        /// </summary>
        private void Start_Android()
        {
            UnityAndroidHelper.Instance.DispatchUiThread(unityActivity =>
            {
                // We should only run the java initialization once
                if (!JavaInitialized)
                {
                    using (AndroidJavaClass cloudServices = new AndroidJavaClass("com.microsoft.CloudServices"))
                    {
                        cloudServices.CallStatic("initialize", unityActivity);
                        JavaInitialized = true;
                    }
                }
                CreateNewCloudSession();
            });
        }

        public Vector3 GetHitPosition_Android()
        {
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon | TrackableHitFlags.FeaturePointWithSurfaceNormal;
            if (Frame.Raycast(0.5f, 0.5f, raycastFilter, out hit))
            {
                return hit.Pose.position;
            }
            return new Vector3();
        }

        long lastFrameProcessedTimeStamp;

        /// <summary>
        /// The spatial anchor session works by mapping the space around the user.
        /// Doing so helps to determine where anchors are located.
        /// Mobile platforms (iOS & Android) require a native call to the camera feed to obtain frames from your platform's AR library.
        /// In contrast, HoloLens is constantly scanning the environment, so there's no need for a specific call like with Mobile.
        /// </summary>
        void ProcessLatestFrame()
        {
            if (cloudSession == null)
            {
                throw new InvalidOperationException("Cloud spatial anchor session is not available.");
            }

            var nativeSession = GoogleARCoreInternal.ARCoreAndroidLifecycleManager.Instance.NativeSession;
            if (nativeSession.FrameHandle == IntPtr.Zero)
            {
                return;
            }

            long latestFrameTimeStamp = nativeSession.FrameApi.GetTimestamp();
            bool newFrameToProcess = latestFrameTimeStamp > lastFrameProcessedTimeStamp;
            if (newFrameToProcess)
            {
                cloudSession.ProcessFrame(nativeSession.FrameHandle);
                lastFrameProcessedTimeStamp = latestFrameTimeStamp;
            }
        }
#endif
        #endregion

        #region Hololens (create anchor not ready)
#if UNITY_WSA || WINDOWS_UWP
        public Vector3 GetHitPosition_Hololens()
        {
            RaycastHit hit;
            // TODO: IMPLEMENT THIS
            /*if (this.TryGazeHitTest(out hit))
            {
                return hit.point;
            }*/

            return new Vector3();
        }
#endif
        #endregion

        #region IOS (not ready)

#if UNITY_IOS
        private Vector3 GetHitPosition_IOS()
        {
            var screenPosition = Camera.main.ScreenToViewportPoint(new Vector3(0.5f, 0.5f));
            ARPoint point = new ARPoint
            {
                x = screenPosition.x,
                y = screenPosition.y
            };
            var hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point, ARHitTestResultType.ARHitTestResultTypeEstimatedHorizontalPlane | ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent);
            if (hitResults.Count > 0)
            {
                // The hitTest method sorts the resulting list by increasing distance from the camera
                // The first hit result will usually be the most relevant when responding to user input
                ARHitTestResult hitResult = hitResults[0];
                hitPosition = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
            }

            return hitPosition;
        }
#endif
        #endregion
    }
}
