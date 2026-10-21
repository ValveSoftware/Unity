using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using Valve.OpenXR.Utils;
#if UNITY_GLTFAST
using GLTFast;
using GLTFast.Logging;
using GLTFast.Schema;
#endif

/// <summary>
/// Shows the render model for one top level user path with glTFast under this transform and animates its nodes. Reloads when the runtime replaces it.
/// With Use Render Model Pose this transform follows the model's pose, so place it under the XR Origin's Camera Offset. Hide When Not Tracked hides
/// the model while untracked.
/// </summary>
public class RenderModelLoader : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Top level user path of the device to show, for example /user/hand/left.")]
    private string userPath = ValveOpenXRRenderModelFeature.UserPaths.LeftHand;

    [SerializeField]
    [Tooltip("Move this object to the pose the runtime reports for the model. " +
             "Turn off to leave it where it is, for example under a tracked controller object.")]
    private bool useRenderModelPose = true;

    [SerializeField]
    [Tooltip("Hide the model while the runtime reports it as not tracked, for example while the controller is asleep.")]
    private bool hideWhenNotTracked = true;

    [SerializeField]
    [Tooltip("The XR Origin's Camera Offset. Render model poses are relative to it. Defaults to the parent. Only used with Use Render Model Pose.")]
    private Transform trackingOrigin;

    // glTF extensions glTFast can import.
    private static readonly string[] SupportedGltfExtensions =
    {
        "KHR_materials_pbrSpecularGlossiness",
        "KHR_materials_transmission",
        "KHR_materials_unlit",
        "KHR_mesh_quantization",
        "KHR_texture_transform",
        "KHR_lights_punctual",
        "EXT_mesh_gpu_instancing",
#if GLTFAST_KTX
        "KHR_texture_basisu",
#endif
#if GLTFAST_DRACO
        "KHR_draco_mesh_compression",
#endif
#if GLTFAST_MESHOPT
        "EXT_meshopt_compression",
#endif
    };

#if UNITY_GLTFAST
    // glTFast's default agent assumes 30 fps when targetFrameRate is unset, as under XR. The loaders share one with a budget that fits a 90 Hz frame.
    private const float GltfImportSecondsPerFrame = 0.002f;
    private static TimeBudgetPerFrameDeferAgent sharedDeferAgent;
#endif

    private ValveOpenXRRenderModelFeature feature;
    // The id of the model being loaded or shown, 0 for none.
    private ulong modelId;
    private ValveOpenXRRenderModelFeature.RenderModel model;
    private Transform modelRoot;
    private GameObject modelRootObject;
    // Index-aligned with animNodeStates. Null when the model has no node with that name.
    private List<Transform> animNodes;
    private List<GameObject> animNodeObjects;
    private List<ValveOpenXRRenderModelFeature.ModelNodeState> animNodeStates;
    private Coroutine modelLoadCoroutine;
    private CancellationTokenSource loadCancellation;
    private bool loadingComplete;
    private ulong failedModelId;
#if UNITY_GLTFAST
    private GltfImport gltf;
    private NodeTrackingInstantiator instantiator;
    private Task<bool> gltfTask;
#endif

    private void Awake()
    {
        OpenXRSettings settings = OpenXRSettings.Instance;
        if (settings != null)
        {
            feature = settings.GetFeature<ValveOpenXRRenderModelFeature>();
        }

        if (feature == null || !feature.enabled)
        {
            Debug.LogError("[RenderModelLoader] The 'Valve Utils: Render Models' OpenXR feature is not enabled - disabling RenderModelLoader.", this);
            enabled = false;
        }

        if (trackingOrigin == null)
        {
            trackingOrigin = transform.parent;
        }
    }

    private void OnEnable()
    {
        if (feature == null)
        {
            return;
        }

        feature.RenderModelsChanged += OnRenderModelsChanged;
        OnRenderModelsChanged();
    }

    private void OnDisable()
    {
        if (feature != null)
        {
            feature.RenderModelsChanged -= OnRenderModelsChanged;
        }

        // Disabling stops the load coroutine. Drop the half loaded model so OnEnable reloads.
        if (modelId != 0 && !loadingComplete)
        {
            ReleaseModel();
        }
    }

    private void Update()
    {
        UpdatePlacement();
        UpdateAnimations();
    }

    private void OnDestroy()
    {
        ReleaseModel();
    }

    /// <summary>Runs on enable and when the offered models change. Loads or reloads the model.</summary>
    private void OnRenderModelsChanged()
    {
        if (!feature.IsAvailable)
        {
            // A new session may reuse ids, so a failure in the old one does not count against it.
            failedModelId = 0;
            ReleaseModel();
            return;
        }

        ulong offeredId = 0;
        feature.TryGetRenderModelId(userPath, SupportedGltfExtensions, out offeredId);

        if (modelId != 0)
        {
            bool modelChanged = offeredId != modelId || (model != null && !model.IsValid);
            if (!modelChanged)
            {
                return;
            }

            ReleaseModel();
        }

        // The feature raises the event again when the session begins.
        if (offeredId != 0 && offeredId != failedModelId && feature.IsSessionRunning)
        {
            modelId = offeredId;
            loadCancellation = new CancellationTokenSource();
            modelLoadCoroutine = StartCoroutine(LoadModelAsync(offeredId, loadCancellation.Token));
        }
    }

    private IEnumerator LoadModelAsync(ulong renderModelId, CancellationToken cancellationToken)
    {
        // Runs on a worker thread. A model arriving after cancellation is destroyed by the feature.
        Task<ValveOpenXRRenderModelFeature.RenderModel> createTask =
            feature.CreateRenderModelAsync(renderModelId, SupportedGltfExtensions, cancellationToken);

        // Always yield once so the stored Coroutine refers to a running one.
        do
        {
            yield return null;
        }
        while (!createTask.IsCompleted);

        if (createTask.Status == TaskStatus.RanToCompletion)
        {
            model = createTask.Result;
        }

        if (model == null)
        {
            // A refusal because the session stopped is retried when it runs again.
            if (feature.IsSessionRunning)
            {
                Debug.LogError($"[RenderModelLoader] Failed to retrieve the render model for {userPath} from the OpenXR runtime.", this);
                failedModelId = renderModelId;
            }
            ReleaseModel();
            yield break;
        }

#if UNITY_GLTFAST
        // The console logger reports import problems, for example a shader that was stripped from the build.
        gltf = new GltfImport(null, GetDeferAgent(), null, new ConsoleLogger());
        gltfTask = gltf.Load(model.GlbData, null, null, cancellationToken);
        while (!gltfTask.IsCompleted)
        {
            yield return null;
        }
        if (!TaskSucceeded(gltfTask, "glTF import"))
        {
            FailLoad();
            yield break;
        }
        model.ReleaseGlbData();

        instantiator = new NodeTrackingInstantiator(gltf, transform);
        gltfTask = gltf.InstantiateMainSceneAsync(instantiator, cancellationToken);
        while (!gltfTask.IsCompleted)
        {
            yield return null;
        }
        if (!TaskSucceeded(gltfTask, "glTF scene instantiation"))
        {
            FailLoad();
            yield break;
        }

        modelRoot = instantiator.SceneTransform;
        if (modelRoot != null)
        {
            modelRootObject = modelRoot.gameObject;
            AdjustModelBasis(modelRoot);
            modelRootObject.SetActive(false);
        }
#else
        Debug.LogWarning("[RenderModelLoader] The Unity glTFast package is not installed. The render model was retrieved but cannot be displayed.", this);
        model.ReleaseGlbData();
#endif

        animNodes = new List<Transform>(model.AnimatableNodeNames.Count);
        animNodeObjects = new List<GameObject>(model.AnimatableNodeNames.Count);
        animNodeStates = new List<ValveOpenXRRenderModelFeature.ModelNodeState>(model.AnimatableNodeNames.Count);
        foreach (string nodeName in model.AnimatableNodeNames)
        {
            Transform node = null;
#if UNITY_GLTFAST
            node = FindGltfNode(gltf, instantiator, nodeName);
            if (node == null)
            {
                Debug.LogWarning($"[RenderModelLoader] Animatable node '{nodeName}' was not found in the glTF asset.", this);
            }
#endif
            animNodes.Add(node);
            animNodeObjects.Add(node != null ? node.gameObject : null);
            animNodeStates.Add(new ValveOpenXRRenderModelFeature.ModelNodeState());
        }

        loadingComplete = true;
        modelLoadCoroutine = null;
    }

    /// <summary>Applies Use Render Model Pose and Hide When Not Tracked each frame.</summary>
    private void UpdatePlacement()
    {
        if (!loadingComplete || modelRoot == null)
        {
            return;
        }

        if (!useRenderModelPose && !hideWhenNotTracked)
        {
            SetActiveIfChanged(modelRootObject, true);
            return;
        }

        bool tracked = feature.TryLocateRenderModel(model, out Pose pose);
        if (tracked && useRenderModelPose)
        {
            if (trackingOrigin != null)
            {
                transform.SetPositionAndRotation(trackingOrigin.TransformPoint(pose.position), trackingOrigin.rotation * pose.rotation);
            }
            else
            {
                transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        SetActiveIfChanged(modelRootObject, tracked || !hideWhenNotTracked);
    }

    private void UpdateAnimations()
    {
        if (!loadingComplete)
        {
            return;
        }
        if (!feature.GetRenderModelStates(model, animNodeStates))
        {
            return;
        }

        for (int i = 0; i < animNodeStates.Count; ++i)
        {
            Transform node = animNodes[i];
            if (node == null)
            {
                continue;
            }

            ValveOpenXRRenderModelFeature.ModelNodeState state = animNodeStates[i];
            SetActiveIfChanged(animNodeObjects[i], state.Visible);
            if (state.Visible)
            {
                Vector3 position = state.Position;
                Quaternion rotation = state.Rotation;
                AdjustLocalAnimUpdate(ref position, ref rotation);
                node.SetLocalPositionAndRotation(position, rotation);
            }
        }
    }

    private void FailLoad()
    {
        if (model != null)
        {
            failedModelId = model.Id;
        }
        ReleaseModel();
    }

    private void ReleaseModel()
    {
        if (modelLoadCoroutine != null)
        {
            StopCoroutine(modelLoadCoroutine);
            modelLoadCoroutine = null;
        }

        if (loadCancellation != null)
        {
            // Also makes the feature destroy a model that is still being created.
            loadCancellation.Cancel();
        }

#if UNITY_GLTFAST
        if (gltf != null)
        {
            // Import jobs may still read the buffers and token source.
            if (gltfTask == null || gltfTask.IsCompleted)
            {
                DeferredDispose.Dispose(gltf, loadCancellation, instantiator);
            }
            else
            {
                // Hide the partially instantiated object until it can be destroyed.
                if (instantiator != null && instantiator.SceneTransform != null)
                {
                    instantiator.SceneTransform.gameObject.SetActive(false);
                }

                DeferredDispose deferred = new DeferredDispose(gltf, loadCancellation, instantiator);
                gltfTask.ContinueWith(deferred.OnImportFinished, TaskScheduler.FromCurrentSynchronizationContext());
            }

            gltf = null;
            instantiator = null;
            loadCancellation = null;
        }
        gltfTask = null;
#endif

        // The feature only reads IsCancellationRequested, which stays valid after Dispose.
        if (loadCancellation != null)
        {
            loadCancellation.Dispose();
            loadCancellation = null;
        }

        modelRoot = null;
        modelRootObject = null;

        if (model != null)
        {
            if (feature != null)
            {
                feature.DestroyRenderModel(model);
            }
            model = null;
        }

        animNodes = null;
        animNodeObjects = null;
        animNodeStates = null;
        loadingComplete = false;
        modelId = 0;
    }

    private static void SetActiveIfChanged(GameObject gameObject, bool active)
    {
        if (gameObject.activeSelf != active)
        {
            gameObject.SetActive(active);
        }
    }

#if UNITY_GLTFAST
    private static IDeferAgent GetDeferAgent()
    {
        if (sharedDeferAgent == null)
        {
            GameObject agentObject = new GameObject("RenderModelLoader glTF budget");
            DontDestroyOnLoad(agentObject);
            sharedDeferAgent = agentObject.AddComponent<TimeBudgetPerFrameDeferAgent>();
        }

        // Fraction of a frame at targetFrameRate, or 30 fps when unset.
        float frameRate = Application.targetFrameRate;
        if (frameRate < 0)
        {
            frameRate = 30;
        }
        sharedDeferAgent.SetFrameBudget(GltfImportSecondsPerFrame * frameRate);
        return sharedDeferAgent;
    }

    private sealed class DeferredDispose
    {
        private readonly GltfImport import;
        private readonly CancellationTokenSource cancellation;
        private readonly NodeTrackingInstantiator instantiator;

        public DeferredDispose(GltfImport import, CancellationTokenSource cancellation, NodeTrackingInstantiator instantiator)
        {
            this.import = import;
            this.cancellation = cancellation;
            this.instantiator = instantiator;
        }

        public void OnImportFinished(Task<bool> task)
        {
            Dispose(import, cancellation, instantiator);
        }

        public static void Dispose(GltfImport import, CancellationTokenSource cancellation, NodeTrackingInstantiator instantiator)
        {
            if (instantiator != null && instantiator.SceneTransform != null)
            {
                Destroy(instantiator.SceneTransform.gameObject);
            }

            import.Dispose();
            if (cancellation != null)
            {
                cancellation.Dispose();
            }
        }
    }

    private bool TaskSucceeded(Task<bool> task, string step)
    {
        if (task.IsCanceled)
        {
            return false;
        }

        if (task.IsFaulted)
        {
            string message = "unknown error";
            if (task.Exception != null)
            {
                message = task.Exception.GetBaseException().Message;
            }
            Debug.LogError($"[RenderModelLoader] {step} failed: {message}", this);
            return false;
        }

        if (!task.Result)
        {
            Debug.LogError($"[RenderModelLoader] {step} failed.", this);
            return false;
        }

        return true;
    }
#endif

#if UNITY_GLTFAST
    /// <summary>
    /// Maps animatable node names to the GameObjects glTFast created, by glTF node index, since hierarchy names may be altered or collide.
    /// </summary>
    private static Transform FindGltfNode(GltfImport gltf, NodeTrackingInstantiator instantiator, string nodeName)
    {
        IReadOnlyList<NodeBase> nodes = gltf.GetSourceRoot().Nodes;
        for (int i = 0; i < nodes.Count; ++i)
        {
            if (nodes[i].name == nodeName && instantiator.TryGetNode(i, out Transform node))
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>glTFast's default instantiator with its node index to GameObject map exposed.</summary>
    private sealed class NodeTrackingInstantiator : GameObjectInstantiator
    {
        public NodeTrackingInstantiator(GltfImport gltf, Transform parent) : base(gltf, parent)
        {
        }

        public bool TryGetNode(int nodeIndex, out Transform node)
        {
            node = null;
            if (m_Nodes != null && m_Nodes.TryGetValue((uint)nodeIndex, out GameObject nodeObject) && nodeObject != null)
            {
                node = nodeObject.transform;
            }

            return node != null;
        }
    }
#endif

    private static void AdjustModelBasis(Transform modelRoot)
    {
        // glTFast mirrors X, OpenXR poses negate Z. A 180 degree turn about Y maps one onto the other.
        modelRoot.localRotation = Quaternion.Euler(0, 180, 0);
        modelRoot.localPosition = Vector3.zero;
    }

    private static void AdjustLocalAnimUpdate(ref Vector3 position, ref Quaternion rotation)
    {
        // Same conversion as AdjustModelBasis, applied to a node's local pose: negate X and Z.
        position.x = -position.x;
        position.z = -position.z;
        rotation.x = -rotation.x;
        rotation.z = -rotation.z;
    }
}
