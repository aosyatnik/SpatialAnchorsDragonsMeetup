using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity.Android;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractSpatialAnchor : MonoBehaviour
{
    // TODO: remove me
    public static string DEBUG_FILTER = "ASA Log:";

    #region protected members

    /// <summary>
    /// Cloud session for storing anchors.
    /// </summary>
    protected CloudSpatialAnchorSession cloudSession = null;

    /// <summary>
    /// Service, that returns/updates anchor id.
    /// </summary>
    protected AzureStorageService storageService = new AzureStorageService();

    /// <summary>
    /// Local created game object.
    /// </summary>
    protected GameObject localAnchorGameObject;

    /// <summary>
    /// Loaded anchor.
    /// </summary>
    protected CloudSpatialAnchor loadedAnchor;

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

    protected void ProcessQueue()
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
        cloudSession.Session = GoogleARCoreInternal.ARCoreAndroidLifecycleManager.Instance.NativeSession.SessionHandle;
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

    // Start is called before the first frame update
    protected void Start()
    {
        Start_Android();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        ProcessQueue();
        ProcessLatestFrame();
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

    long lastFrameProcessedTimeStamp;

    /// <summary>
    /// The spatial anchor session works by mapping the space around the user.
    /// Doing so helps to determine where anchors are located.
    /// Mobile platforms (iOS & Android) require a native call to the camera feed to obtain frames from your platform's AR library.
    /// In contrast, HoloLens is constantly scanning the environment, so there's no need for a specific call like with Mobile.
    /// </summary>
    protected void ProcessLatestFrame()
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

    protected void InstantiateLocalGameObject(Vector3 position, Quaternion rotation)
    {
        localAnchorGameObject = GameObject.Instantiate(AnchoredObjectPrefab, position, rotation);

        // Animate
        localAnchorGameObject.AddComponent<MeshRenderer>();
        Animator animator = localAnchorGameObject.GetComponent<Animator>();
        var controller = Resources.Load("Dragon Animator Controller") as RuntimeAnimatorController;
        animator.runtimeAnimatorController = controller;
        localAnchorGameObject.AddComponent<DragonAnimation>();

        localAnchorGameObject.AddComponent<DragonAI>();
    }
}
