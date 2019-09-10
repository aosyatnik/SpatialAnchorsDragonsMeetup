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
    public class SpatialAnchors : MonoBehaviour
    {
        // TODO: remove me
        public static string DEBUG_FILTER = "ASA Log:";

        #region private members
        /// <summary>
        /// Cloud session for storing anchors.
        /// </summary>
        private CloudSpatialAnchorSession cloudSession = null;

        /// <summary>
        /// Service, that returns/updates anchor id.
        /// </summary>
        private AzureStorageService storageService = new AzureStorageService();

        /// <summary>
        /// Local created game object.
        /// </summary>
        private GameObject localAnchorGameObject;

        /// <summary>
        /// Local created game object position.
        /// </summary>
        private Vector3 localAnchorPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// Save anchor is new position has some distance from the old one.
        /// </summary>
        private double minPositionChangeDistance = 1;

        /// <summary>
        /// When next time anchor will be updated.
        /// </summary>
        private int nextUpdate = 0;

        /// <summary>
        /// 1 second, anchor will be updated once per second.
        /// </summary>
        private int duration = 1;

        /// <summary>
        /// Loaded anchor.
        /// </summary>
        private CloudSpatialAnchor loadedAnchor;

        private string currentlyLoadingAnchorId = "";
        #endregion

        #region public members
        /// <summary>
        /// Scanned room percent.
        /// </summary>
        public float ScannedPercent;

        /// <summary>
        /// Prefab, that is used for anchor creation. I.e. dragon.
        /// </summary>
        public GameObject AnchoredObjectPrefab;

        #endregion

        #region ui thread queue

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

        #endregion

        #region Cloud session

        private void CreateNewCloudSession()
        {
            cloudSession = new CloudSpatialAnchorSession();
            cloudSession.Configuration.AccountId = @"2c54fa04-44c0-4b61-a848-4f86395311aa";
            cloudSession.Configuration.AccountKey = @"JOmr5g1hpJyCOVqFs95MNQ5B1Z5w7S23mFoBeKRSm/I=";
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

        #endregion

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
#if UNITY_ANDROID || UNITY_IOS
            Pose anchorPose = foundAnchor.GetAnchorPose();
            Debug.Log("ASA log: new position:" + anchorPose.position);
            localAnchorGameObject.GetComponent<DragonAI>().ChangePosition(anchorPose.position);
#elif UNITY_WSA || WINDOWS_UWP
            // Hololens is using SetNativeSpatialAnchorPtr for getting position.
            localAnchorGameObject.GetComponent<WorldAnchor>().SetNativeSpatialAnchorPtr(foundAnchor.LocalAnchor);
#endif
        }

        /// <summary>
        /// Save new position to anchors.
        /// </summary>
        private void Dragon_OnPositionChanged(Vector3 newPosition)
        {
            UpdatePosition(newPosition);
        }

        private async void Dragon_OnNextPositionChanged(Vector3 nextPosition)
        {
#if UNITY_WSA || WINDOWS_UWP
            await CreateAnchorAsync(nextPosition);
#endif
        }

        private void InstantiateLocalGameObject(Vector3 position, Quaternion rotation)
        {
            localAnchorGameObject = GameObject.Instantiate(AnchoredObjectPrefab, position, rotation);

            // Animate
            localAnchorGameObject.AddComponent<MeshRenderer>();
            Animator animator = localAnchorGameObject.GetComponent<Animator>();
            var controller = Resources.Load("Dragon Animator Controller") as RuntimeAnimatorController;
            animator.runtimeAnimatorController = controller;
            localAnchorGameObject.AddComponent<DragonAnimation>();

            localAnchorGameObject.AddComponent<DragonAI>();
            localAnchorGameObject.GetComponent<DragonAI>().Dragon.OnPositionChanged += Dragon_OnPositionChanged;
            localAnchorGameObject.GetComponent<DragonAI>().Dragon.OnNextPositionChanged += Dragon_OnNextPositionChanged;
        }

        private void UpdatePosition(Vector3 newPosition)
        {
            localAnchorGameObject.transform.position = newPosition;
        }

        #region Android
#if UNITY_ANDROID

        private void Start()
        {
            Start_Android();
        }

        private void Update()
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

        #region Hololens
#if UNITY_WSA || WINDOWS_UWP
        private void Start()
        {
            CreateNewCloudSession();
            CaptureGestures();
        }

        private void Update()
        {
            ProcessQueue();
        }

        /// <summary>
        /// Use the recognizer to detect air taps.
        /// </summary>
        private GestureRecognizer recognizer;
        
        private void CaptureGestures()
        {
            recognizer = new GestureRecognizer();

            recognizer.StartCapturingGestures();

            recognizer.SetRecognizableGestures(GestureSettings.Tap);

            recognizer.Tapped += HandleTap;
        }

        private void HandleTap(TappedEventArgs tapEvent)
        {
            Debug.Log(DEBUG_FILTER + "hololens will create a new anchor.");

            // Construct a Ray using forward direction of the HoloLens.
            Ray GazeRay = new Ray(tapEvent.headPose.position, tapEvent.headPose.forward);

            // Raycast to get the hit point in the real world.
            RaycastHit hitInfo;
            Physics.Raycast(GazeRay, out hitInfo, float.MaxValue);

            if (localAnchorGameObject is null)
            {
                InstantiateLocalGameObject(hitInfo.point, Quaternion.AngleAxis(0, Vector3.up));
            }
            
            CreateAnchorAsync(hitInfo.point);
        }

        private async Task CreateAnchorAsync(Vector3 newPosition)
        {
            // Lock game object. While anchor is saved.
            localAnchorGameObject.AddARAnchor();

            if (loadedAnchor != null)
            {
                // Remove old anchor.
                await cloudSession.DeleteAnchorAsync(loadedAnchor);
            }

            // Save to server.
            CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
            cloudAnchor.LocalAnchor = localAnchorGameObject.GetNativeAnchorPointer();
            cloudAnchor.AppProperties[@"label"] = @"Dragon";
            await cloudSession.CreateAnchorAsync(cloudAnchor);
            await storageService.PostAnchorId(cloudAnchor.Identifier);
            Debug.Log(DEBUG_FILTER + $"Created a cloud anchor with ID={cloudAnchor.Identifier}");

            // Unlock game object.
            localAnchorGameObject.RemoveARAnchor();
            localAnchorGameObject.GetComponent<DragonAI>().ChangePosition(newPosition);
            Debug.Log(DEBUG_FILTER + "saved position" + newPosition + " to server");
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
