using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Access to XR_EXT_render_model and XR_EXT_interaction_render_model: controller glTF data, node names, model pose and node poses.
    /// Importing the glTF is up to the caller. See the Render Model sample.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: Render Models",
        Desc = "Retrieve controller render models (glTF) and their animatable node states from the OpenXR runtime.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        Version = "1.0.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        FeatureId = featureId,
        OpenxrExtensionStrings = ExtensionRenderModel + " " + ExtensionInteractionRenderModel + " " + ExtensionUuid
    )]
#endif
    public partial class ValveOpenXRRenderModelFeature : OpenXRFeature
    {
        public const string featureId = "com.valvesoftware.openxr.utils.render_models";

        private const string ExtensionRenderModel = "XR_EXT_render_model";
        private const string ExtensionInteractionRenderModel = "XR_EXT_interaction_render_model";
        private const string ExtensionUuid = "XR_EXT_uuid";

        private const string LogPrefix = "[" + nameof(ValveOpenXRRenderModelFeature) + "] ";

        /// <summary>Top level user paths from the OpenXR core specification. Other paths show up in <see cref="GetOfferedRenderModels"/>.</summary>
        public static class UserPaths
        {
            public const string Head = "/user/head";
            public const string LeftHand = "/user/hand/left";
            public const string RightHand = "/user/hand/right";
            public const string Gamepad = "/user/gamepad";
        }

        /// <summary>A model the runtime currently offers.</summary>
        public sealed class OfferedRenderModel
        {
            /// <summary>Runtime id, valid for <see cref="TryCreateRenderModel(ulong, IList{string}, out RenderModel)"/>.</summary>
            public ulong Id { get; internal set; }

            /// <summary>Top level user paths the model is bound to. Several models may list the same path. Empty when unknown.</summary>
            public IReadOnlyList<string> UserPaths { get; internal set; }

            /// <summary>The one path in <see cref="UserPaths"/> whose pose the model follows, or null when the runtime reports none.</summary>
            public string PoseUserPath { get; internal set; }

            internal ulong[] UserPathHandles;
            internal ulong PoseUserPathHandle;

            internal OfferedRenderModel(ulong id)
            {
                Id = id;
                UserPaths = new string[0];
                UserPathHandles = new ulong[0];
            }
        }

        /// <summary>
        /// A model from <see cref="TryCreateRenderModel"/>. Destroy it with <see cref="DestroyRenderModel"/>. Invalid once the session is destroyed.
        /// </summary>
        public sealed class RenderModel
        {
            /// <summary>Runtime id the model was created from. Changes when the runtime replaces the model.</summary>
            public ulong Id { get; internal set; }

            /// <summary>Top level user paths the model was bound to when it was created. See <see cref="OfferedRenderModel.UserPaths"/>.</summary>
            public IReadOnlyList<string> UserPaths { get; internal set; }

            /// <summary>The user path the model's pose follows, or null. See <see cref="OfferedRenderModel.PoseUserPath"/>.</summary>
            public string PoseUserPath { get; internal set; }

            /// <summary>Identifies the glTF asset. Stable across sessions, so it can key an on-disk or prefab cache.</summary>
            public Guid CacheId { get; internal set; }

            /// <summary>Names of the animated glTF nodes, in the order used by <see cref="GetRenderModelStates"/>.</summary>
            public List<string> AnimatableNodeNames { get; internal set; }

            /// <summary>The model as a glTF binary (GLB) file. Null after <see cref="ReleaseGlbData"/>.</summary>
            public byte[] GlbData { get; internal set; }

            public bool IsValid
            {
                get { return Handle != 0; }
            }

            /// <summary>Drops <see cref="GlbData"/> once it has been imported.</summary>
            public void ReleaseGlbData()
            {
                GlbData = null;
            }

            internal ulong Handle;
            internal ulong Space;
            internal bool LocateFailureLogged;
            internal bool StateFailureLogged;
        }

        /// <summary>
        /// Pose and visibility of one animatable node, relative to its parent. Converted to Unity space (Z negated). See the sample for the glTF side.
        /// </summary>
        public class ModelNodeState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public bool Visible;
        }

        // Sized so enumerations usually need one call.
        private const int InitialIdCapacity = 16;
        private const int InitialPathCapacity = 4;

        private ulong _xrInstance;
        private ulong _xrSession;
        private bool _functionsResolved;
        private bool _sessionRunning;
        // Session begin counter, so stale worker results are ignored.
        private int _sessionGeneration;
        private int _displayTimeFrame = -1;
        private long _displayTime;

        // Models handed out to callers. Destroyed with the session.
        private readonly List<RenderModel> _renderModels = new List<RenderModel>();
        private XrRenderModelNodeStateEXT[] _nodeStateBuffer = new XrRenderModelNodeStateEXT[0];

        // Offered ids in runtime order with their paths. Refreshed on a worker thread (about 7 ms on the Steam Frame) at session begin, after
        // XrEventDataInteractionRenderModelsChangedEXT, and after XrEventDataInteractionProfileChanged for the paths.
        private ulong[] _offeredRenderModelIds = new ulong[0];
        private Dictionary<ulong, OfferedRenderModel> _offeredRenderModels = new Dictionary<ulong, OfferedRenderModel>();
        private bool _offeredRenderModelsDirty;
        private bool _userPathsDirty;
        // glTF extensions from the most recent query. Refreshes wait for the first query.
        private string[] _gltfExtensions;
        private OfferedRenderModelsRefresh _refresh;
        // Failed refreshes retry once per frame at most and log once.
        private bool _refreshFailed;
        private int _refreshFailedFrame = -1;
        private bool _refreshFailureLogged;
        // Worker thread work. OnSessionDestroy waits for it.
        private readonly List<BackgroundWork> _backgroundWork = new List<BackgroundWork>();
        private XrPollEventCallback _onRenderModelsChanged;
        private XrPollEventCallback _onInteractionProfileChanged;

        // xrStringToPath results, valid for the instance.
        private readonly Dictionary<string, ulong> _pathCache = new Dictionary<string, ulong>();

        /// <summary>True while a session exists and the runtime supports the render model extensions.</summary>
        public bool IsAvailable
        {
            get { return _xrSession != 0 && _functionsResolved; }
        }

        /// <summary>True while the session is running. Models can only be created then.</summary>
        public bool IsSessionRunning
        {
            get { return _sessionRunning; }
        }

        /// <summary>
        /// Raised on the main thread when the session begins, when the offered models change and when the session is destroyed, which invalidates
        /// every <see cref="RenderModel"/>. Query <see cref="TryGetRenderModelId"/> or <see cref="GetOfferedRenderModels"/> from the handler, and once
        /// after subscribing.
        /// </summary>
        public event Action RenderModelsChanged;

        /// <summary>True when the runtime offers a render model for the user path. Cached, so cheap to poll every frame.</summary>
        public bool IsRenderModelAvailable(string userPath, IList<string> supportedGltfExtensions)
        {
            return TryGetRenderModelId(userPath, supportedGltfExtensions, out _);
        }

        /// <summary>
        /// Lists every offered render model with its top level user paths, for example to show trackers. Clears <paramref name="results"/> first.
        /// Cached, so cheap to call every frame. Empty until the first refresh raises <see cref="RenderModelsChanged"/>.
        /// </summary>
        /// <param name="supportedGltfExtensions">glTF extensions the caller can import. Use the same list for the whole session.</param>
        public bool GetOfferedRenderModels(IList<string> supportedGltfExtensions, List<OfferedRenderModel> results)
        {
            if (results == null)
            {
                return false;
            }

            results.Clear();
            if (!IsAvailable)
            {
                return false;
            }

            RefreshOfferedRenderModels(supportedGltfExtensions);
            if (_refreshFailed)
            {
                return false;
            }

            foreach (ulong id in _offeredRenderModelIds)
            {
                results.Add(_offeredRenderModels[id]);
            }

            return true;
        }

        /// <summary>
        /// Finds the id of the model offered for a top level user path. The id changes when the runtime replaces the model, so compare it against
        /// <see cref="RenderModel.Id"/> to detect reloads. Returns false with id 0 when none is offered. Cached, so cheap to call every frame.
        /// </summary>
        /// <remarks>
        /// When several models list the path, the one whose pose follows it is preferred. See <see cref="GetOfferedRenderModels"/> for all of them.
        /// </remarks>
        /// <param name="userPath">A top level user path, for example "/user/hand/left".</param>
        /// <param name="supportedGltfExtensions">glTF extensions the caller can import. Use the same list for the whole session.</param>
        public bool TryGetRenderModelId(string userPath, IList<string> supportedGltfExtensions, out ulong renderModelId)
        {
            renderModelId = 0;
            if (!IsAvailable || string.IsNullOrEmpty(userPath))
            {
                return false;
            }

            ulong targetPath = GetPath(userPath);
            if (targetPath == XR_NULL_PATH)
            {
                return false;
            }

            RefreshOfferedRenderModels(supportedGltfExtensions);
            if (_refreshFailed)
            {
                return false;
            }

            ulong firstMatch = 0;
            foreach (ulong id in _offeredRenderModelIds)
            {
                OfferedRenderModel offered = _offeredRenderModels[id];
                if (Array.IndexOf(offered.UserPathHandles, targetPath) < 0)
                {
                    continue;
                }

                if (offered.PoseUserPathHandle == targetPath)
                {
                    renderModelId = id;
                    return true;
                }

                if (firstMatch == 0)
                {
                    firstMatch = id;
                }
            }

            renderModelId = firstMatch;
            return firstMatch != 0;
        }

        /// <summary>
        /// Creates the render model offered for a top level user path and copies its glTF data out of the runtime.
        /// </summary>
        /// <param name="userPath">A top level user path, for example <see cref="UserPaths.RightHand"/>.</param>
        /// <param name="supportedGltfExtensions">glTF extensions the caller can import. Pass an empty list for none.</param>
        /// <param name="renderModel">The created model. Destroy it with <see cref="DestroyRenderModel"/>.</param>
        /// <returns>True on success. On failure nothing is left allocated in the runtime.</returns>
        public bool TryCreateRenderModel(string userPath, IList<string> supportedGltfExtensions, out RenderModel renderModel)
        {
            ulong renderModelId = 0;
            if (IsAvailable && !TryGetRenderModelId(userPath, supportedGltfExtensions, out renderModelId))
            {
                renderModel = null;
                Debug.LogWarning(LogPrefix + $"No render model is currently available for {userPath}.");
                return false;
            }

            // The id overload reports why models cannot be created.
            return TryCreateRenderModel(renderModelId, supportedGltfExtensions, out renderModel);
        }

        /// <summary>
        /// Creates the render model with the given id and copies its glTF data out of the runtime. Blocks about 2 ms on the Steam Frame for a 2 MB model.
        /// <see cref="CreateRenderModelAsync"/> does the same on a worker thread.
        /// </summary>
        /// <param name="renderModelId">The id of the model the runtime offers.</param>
        /// <param name="supportedGltfExtensions">glTF extensions the caller can import. Pass an empty list for none.</param>
        /// <param name="renderModel">The created model. Destroy it with <see cref="DestroyRenderModel"/>.</param>
        /// <returns>True on success. On failure nothing is left allocated in the runtime.</returns>
        public bool TryCreateRenderModel(ulong renderModelId, IList<string> supportedGltfExtensions, out RenderModel renderModel)
        {
            renderModel = null;
            if (!CanCreateRenderModel(renderModelId))
            {
                return false;
            }

            RenderModelCreation creation = NewRenderModelCreation(renderModelId, supportedGltfExtensions);
            creation.Execute();
            renderModel = CompleteRenderModelCreation(creation);
            return renderModel != null;
        }

        /// <summary>
        /// Like <see cref="TryCreateRenderModel(ulong, IList{string}, out RenderModel)"/>, on a worker thread. Call on the main thread. Completes on the
        /// main thread with the model, or null on failure, session destroy or cancellation. A model arriving after cancellation is destroyed.
        /// </summary>
        /// <param name="renderModelId">The id of the model the runtime offers.</param>
        /// <param name="supportedGltfExtensions">glTF extensions the caller can import. Pass an empty list for none.</param>
        /// <param name="cancellationToken">Cancels the request. The runtime calls already running still finish.</param>
        public async Task<RenderModel> CreateRenderModelAsync(ulong renderModelId, IList<string> supportedGltfExtensions,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested || !CanCreateRenderModel(renderModelId))
            {
                return null;
            }

            RenderModelCreation creation = NewRenderModelCreation(renderModelId, supportedGltfExtensions);
            await StartBackgroundWork(creation);
            if (!TakeBackgroundResult(creation))
            {
                return null;
            }

            RenderModel renderModel = CompleteRenderModelCreation(creation);
            if (renderModel != null && cancellationToken.IsCancellationRequested)
            {
                DestroyRenderModel(renderModel);
                return null;
            }

            return renderModel;
        }

        /// <summary>
        /// Locates the model's origin in Unity's tracking space (the XR Origin's Camera Offset). Returns false when the model is not tracked, for
        /// example while the session is not focused, in which case it should not be rendered.
        /// </summary>
        public bool TryLocateRenderModel(RenderModel renderModel, out Pose pose)
        {
            pose = Pose.identity;
            if (!IsAvailable || renderModel == null || renderModel.Space == 0 || !_renderModels.Contains(renderModel))
            {
                return false;
            }

            ulong appSpace = GetCurrentAppSpace();
            if (appSpace == 0)
            {
                return false;
            }

            if (!TryGetDisplayTime(out long displayTime))
            {
                return false;
            }

            XrSpaceLocation location = new XrSpaceLocation { type = XrStructureType.SpaceLocation };
            if (!CheckFrameResult(_xrLocateSpace(renderModel.Space, appSpace, displayTime, ref location), "xrLocateSpace", ref renderModel.LocateFailureLogged))
            {
                return false;
            }

            const XrSpaceLocationFlags trackedBits = XrSpaceLocationFlags.OrientationTracked | XrSpaceLocationFlags.PositionTracked;
            if (((XrSpaceLocationFlags)location.locationFlags & trackedBits) != trackedBits)
            {
                return false;
            }

            pose = new Pose(location.pose.Position.AsVector3(), location.pose.Orientation.AsQuaternion());
            return true;
        }

        /// <summary>
        /// Fills <paramref name="nodeStates"/> with each animatable node's pose and visibility, one entry per name in
        /// <see cref="RenderModel.AnimatableNodeNames"/>.
        /// </summary>
        public bool GetRenderModelStates(RenderModel renderModel, List<ModelNodeState> nodeStates)
        {
            if (!IsAvailable || renderModel == null || renderModel.Handle == 0 || !_renderModels.Contains(renderModel) || nodeStates == null)
            {
                return false;
            }

            int nodeCount = nodeStates.Count;
            if (nodeCount != renderModel.AnimatableNodeNames.Count)
            {
                Debug.LogError(LogPrefix + $"nodeStates has {nodeCount} entries but the model has {renderModel.AnimatableNodeNames.Count} animatable nodes.");
                return false;
            }

            if (nodeCount == 0)
            {
                return true;
            }

            if (!TryGetDisplayTime(out long displayTime))
            {
                return false;
            }

            if (_nodeStateBuffer.Length < nodeCount)
            {
                _nodeStateBuffer = new XrRenderModelNodeStateEXT[nodeCount];
            }

            XrRenderModelStateGetInfoEXT getInfo = new XrRenderModelStateGetInfoEXT
            {
                type = XrStructureType.RenderModelStateGetInfoEXT,
                displayTime = displayTime
            };

            XrResult result;
            unsafe
            {
                fixed (XrRenderModelNodeStateEXT* nodeStateBuffer = _nodeStateBuffer)
                {
                    XrRenderModelStateEXT state = new XrRenderModelStateEXT
                    {
                        type = XrStructureType.RenderModelStateEXT,
                        nodeStateCount = (uint)nodeCount,
                        nodeStates = (IntPtr)nodeStateBuffer
                    };
                    result = _xrGetRenderModelStateEXT(renderModel.Handle, ref getInfo, ref state);
                }
            }

            if (!CheckFrameResult(result, "xrGetRenderModelStateEXT", ref renderModel.StateFailureLogged))
            {
                return false;
            }

            for (int i = 0; i < nodeCount; ++i)
            {
                ModelNodeState nodeState = nodeStates[i];
                if (nodeState == null)
                {
                    nodeState = new ModelNodeState();
                    nodeStates[i] = nodeState;
                }
                nodeState.Visible = _nodeStateBuffer[i].isVisible != 0;
                nodeState.Position = _nodeStateBuffer[i].nodePose.Position.AsVector3();
                nodeState.Rotation = _nodeStateBuffer[i].nodePose.Orientation.AsQuaternion();
            }

            return true;
        }

        /// <summary>Asks the runtime which of the given top level user paths the model's pose follows. Returns false with null when none.</summary>
        /// <param name="candidateUserPaths">Distinct top level user paths to choose from.</param>
        public bool TryGetRenderModelPoseUserPath(RenderModel renderModel, IList<string> candidateUserPaths, out string userPath)
        {
            userPath = null;
            if (!IsAvailable || renderModel == null || renderModel.Handle == 0 || !_renderModels.Contains(renderModel) ||
                candidateUserPaths == null || candidateUserPaths.Count == 0)
            {
                return false;
            }

            ulong[] candidates = new ulong[candidateUserPaths.Count];
            for (int i = 0; i < candidates.Length; ++i)
            {
                ulong path = XR_NULL_PATH;
                if (!string.IsNullOrEmpty(candidateUserPaths[i]))
                {
                    path = GetPath(candidateUserPaths[i]);
                }

                if (path == XR_NULL_PATH || Array.IndexOf(candidates, path, 0, i) >= 0)
                {
                    Debug.LogError(LogPrefix + $"'{candidateUserPaths[i]}' is not a valid user path or is listed more than once.");
                    return false;
                }

                candidates[i] = path;
            }

            ulong posePath = GetRenderModelPoseUserPath(renderModel.Handle, candidates);
            if (posePath == XR_NULL_PATH)
            {
                return false;
            }

            userPath = PathToString(posePath);
            return true;
        }

        /// <summary>Destroys a render model. Returns false if it was already destroyed, for example with the session.</summary>
        public bool DestroyRenderModel(RenderModel renderModel)
        {
            if (renderModel == null || !_renderModels.Remove(renderModel))
            {
                return false;
            }

            ReleaseRenderModel(renderModel);
            return true;
        }

        #region OpenXRFeature overrides

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            _xrInstance = xrInstance;
            _functionsResolved = false;

            if (!OpenXRRuntime.IsExtensionEnabled(ExtensionRenderModel) || !OpenXRRuntime.IsExtensionEnabled(ExtensionInteractionRenderModel))
            {
                Debug.LogWarning(LogPrefix + $"The OpenXR runtime does not support {ExtensionRenderModel} and {ExtensionInteractionRenderModel}. " +
                    "Render models will not be available.");
                return false;
            }

            // Every feature's hook is in the chain by now.
            if (!ValveOpenXRDisplayTimeFeature.IsHookInstalled)
            {
                Debug.LogError(LogPrefix + "The Valve Utils: Display Time feature is not enabled. It provides the frame's predicted display time, " +
                    "which render model poses and node states are located at. Render models will not be available.");
                return false;
            }

            _functionsResolved = ResolveInstanceProcs();
            if (_functionsResolved)
            {
                if (_onRenderModelsChanged == null)
                {
                    unsafe
                    {
                        _onRenderModelsChanged = OnRenderModelsChanged;
                        _onInteractionProfileChanged = OnInteractionProfileChanged;
                    }
                }
                PollEventRouter.TrySubscribeToEventType(XrStructureType.EventDataInteractionRenderModelsChangedEXT, _onRenderModelsChanged);
                PollEventRouter.TrySubscribeToEventType(XrStructureType.EventDataInteractionProfileChanged, _onInteractionProfileChanged);
            }

            return _functionsResolved;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            if (_onRenderModelsChanged != null)
            {
                PollEventRouter.TryUnsubscribeFromEventType(XrStructureType.EventDataInteractionRenderModelsChangedEXT, _onRenderModelsChanged);
                PollEventRouter.TryUnsubscribeFromEventType(XrStructureType.EventDataInteractionProfileChanged, _onInteractionProfileChanged);
            }

            // If OnSessionDestroy did not run, the handles are dead. Drop the models without calling the runtime.
            bool hadSession = _xrSession != 0 || _renderModels.Count > 0;
            AbandonBackgroundWork(false);
            foreach (RenderModel renderModel in _renderModels)
            {
                renderModel.Space = 0;
                renderModel.Handle = 0;
            }
            ResetSessionState();

            ClearInstanceProcs();
            _pathCache.Clear();
            _gltfExtensions = null;
            _functionsResolved = false;
            _xrInstance = 0;

            if (hadSession)
            {
                RaiseRenderModelsChanged();
            }
        }

        protected override void OnSessionCreate(ulong xrSession)
        {
            _xrSession = xrSession;
        }

        protected override void OnSessionBegin(ulong xrSession)
        {
            _sessionRunning = true;
            ++_sessionGeneration;
            _displayTimeFrame = -1;
            // The runtime may not queue a change event for the set it already offers.
            _offeredRenderModelsDirty = true;
            StartRefresh();
            RaiseRenderModelsChanged();
        }

        protected override void OnSessionEnd(ulong xrSession)
        {
            _sessionRunning = false;
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            // Runs before xrDestroySession. Worker work finishes first and its handles are released here.
            AbandonBackgroundWork(_functionsResolved);
            if (_functionsResolved)
            {
                foreach (RenderModel renderModel in _renderModels)
                {
                    ReleaseRenderModel(renderModel);
                }
            }

            ResetSessionState();
            RaiseRenderModelsChanged();
        }

        private void ResetSessionState()
        {
            _renderModels.Clear();
            _offeredRenderModelIds = new ulong[0];
            _offeredRenderModels = new Dictionary<ulong, OfferedRenderModel>();
            _offeredRenderModelsDirty = false;
            _userPathsDirty = false;
            _refresh = null;
            _refreshFailed = false;
            _refreshFailedFrame = -1;
            _refreshFailureLogged = false;
            _sessionRunning = false;
            _xrSession = 0;
        }

        // Queued by xrSyncActions when the offered set changes.
        private unsafe void OnRenderModelsChanged(XrEventDataBaseHeader* eventData)
        {
            _offeredRenderModelsDirty = true;
            StartRefreshOrNotify();
        }

        // Model paths follow the interaction profile, so every offered model is described again.
        private unsafe void OnInteractionProfileChanged(XrEventDataBaseHeader* eventData)
        {
            _offeredRenderModelsDirty = true;
            _userPathsDirty = true;
            StartRefreshOrNotify();
        }

        // No refresh to wait for, so tell handlers now and let their query start one.
        private void StartRefreshOrNotify()
        {
            StartRefresh();
            if (_refresh == null)
            {
                RaiseRenderModelsChanged();
            }
        }

        // Must not throw into Unity's callbacks.
        private void RaiseRenderModelsChanged()
        {
            Action handlers = RenderModelsChanged;
            if (handlers == null)
            {
                return;
            }

            try
            {
                handlers();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

#if UNITY_EDITOR
        protected override void GetValidationChecks(List<ValidationRule> results, BuildTargetGroup target)
        {
            results.Add(new ValidationRule(this)
            {
                message = "Render Models requires the Valve Utils: Display Time feature, which provides the frame's predicted display time.",
                checkPredicate = () =>
                {
                    ValveOpenXRDisplayTimeFeature displayTime = GetDisplayTimeFeature(target);
                    return displayTime != null && displayTime.enabled;
                },
                error = true,
                fixIt = () =>
                {
                    ValveOpenXRDisplayTimeFeature displayTime = GetDisplayTimeFeature(target);
                    if (displayTime != null)
                    {
                        displayTime.enabled = true;
                        EditorUtility.SetDirty(displayTime);
                    }
                },
                fixItAutomatic = true,
                fixItMessage = "Enable the Valve Utils: Display Time feature."
            });
        }

        private static ValveOpenXRDisplayTimeFeature GetDisplayTimeFeature(BuildTargetGroup target)
        {
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            if (settings == null)
            {
                return null;
            }

            return settings.GetFeature<ValveOpenXRDisplayTimeFeature>();
        }
#endif

        #endregion

        #region Render model helpers

        private void ReleaseRenderModel(RenderModel renderModel)
        {
            if (renderModel.Space != 0)
            {
                CheckResult(_xrDestroySpace(renderModel.Space), "xrDestroySpace");
                renderModel.Space = 0;
            }

            if (renderModel.Handle != 0)
            {
                CheckResult(_xrDestroyRenderModelEXT(renderModel.Handle), "xrDestroyRenderModelEXT");
                renderModel.Handle = 0;
            }
        }

        /// <summary>Stores the caller's glTF extensions and starts a refresh when the runtime reported a change.</summary>
        private void RefreshOfferedRenderModels(IList<string> supportedGltfExtensions)
        {
            if (!HasSameGltfExtensions(supportedGltfExtensions))
            {
                _gltfExtensions = CopyGltfExtensions(supportedGltfExtensions);
            }

            StartRefresh();
        }

        private bool HasSameGltfExtensions(IList<string> supportedGltfExtensions)
        {
            int count = 0;
            if (supportedGltfExtensions != null)
            {
                count = supportedGltfExtensions.Count;
            }

            if (_gltfExtensions == null || _gltfExtensions.Length != count)
            {
                return false;
            }

            for (int i = 0; i < count; ++i)
            {
                if (!string.Equals(_gltfExtensions[i], supportedGltfExtensions[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Copied so the worker does not see later changes.
        private static string[] CopyGltfExtensions(IList<string> supportedGltfExtensions)
        {
            if (supportedGltfExtensions == null)
            {
                return new string[0];
            }

            string[] copy = new string[supportedGltfExtensions.Count];
            supportedGltfExtensions.CopyTo(copy, 0);
            return copy;
        }

        /// <summary>
        /// Enumerates the offered ids on a worker thread when the runtime reported a change. Only new ids, ids without paths and ids whose profile
        /// changed are described. Needs a running session, so the cache is kept while stopped.
        /// </summary>
        private void StartRefresh()
        {
            if (!_offeredRenderModelsDirty || !_sessionRunning || _refresh != null || _gltfExtensions == null ||
                _refreshFailedFrame == Time.frameCount)
            {
                return;
            }

            bool describeAll = _userPathsDirty;
            _offeredRenderModelsDirty = false;
            _userPathsDirty = false;

            List<ulong> describedIds = new List<ulong>();
            if (!describeAll)
            {
                foreach (KeyValuePair<ulong, OfferedRenderModel> entry in _offeredRenderModels)
                {
                    if (entry.Value.UserPathHandles.Length > 0)
                    {
                        describedIds.Add(entry.Key);
                    }
                }
            }

            _refresh = new OfferedRenderModelsRefresh(this, _gltfExtensions, describedIds, describeAll);
            RunRefresh(_refresh);
        }

        private async void RunRefresh(OfferedRenderModelsRefresh refresh)
        {
            await StartBackgroundWork(refresh);
            if (!TakeBackgroundResult(refresh))
            {
                return;
            }

            _refresh = null;
            ApplyRefresh(refresh);
        }

        private void ApplyRefresh(OfferedRenderModelsRefresh refresh)
        {
            if (!refresh.Result.IsSuccess())
            {
                // Retried by the next query or change event.
                _offeredRenderModelsDirty = true;
                if (refresh.DescribeAll)
                {
                    _userPathsDirty = true;
                }

                if (refresh.Result == XrResult.SessionNotRunning)
                {
                    // The runtime stopped the session ahead of OnSessionEnd. The cache stays valid.
                    if (refresh.Generation == _sessionGeneration)
                    {
                        _sessionRunning = false;
                    }

                    StartRefresh();
                    return;
                }

                _refreshFailed = true;
                _refreshFailedFrame = Time.frameCount;
                if (!_refreshFailureLogged)
                {
                    _refreshFailureLogged = true;
                    Debug.LogError(LogPrefix +
                        $"xrEnumerateInteractionRenderModelIdsEXT failed with XrResult {refresh.Result}. Retrying at most once per frame.");
                }
                return;
            }

            _refreshFailed = false;
            _refreshFailureLogged = false;

            Dictionary<ulong, OfferedRenderModel> offeredRenderModels = new Dictionary<ulong, OfferedRenderModel>(refresh.Ids.Length);
            foreach (ulong id in refresh.Ids)
            {
                if (refresh.Descriptions.TryGetValue(id, out OfferedRenderModel offered))
                {
                    ResolveUserPaths(offered);
                }
                else if (!_offeredRenderModels.TryGetValue(id, out offered))
                {
                    offered = new OfferedRenderModel(id);
                }

                offeredRenderModels[id] = offered;
            }

            _offeredRenderModels = offeredRenderModels;
            _offeredRenderModelIds = refresh.Ids;

            // Picks up a change the runtime reported while this refresh ran.
            StartRefresh();
            RaiseRenderModelsChanged();
        }

        /// <summary>
        /// Reads a model's user paths and pose path through its handle. Worker thread safe, path strings come from <see cref="ResolveUserPaths"/>.
        /// </summary>
        private OfferedRenderModel DescribeRenderModel(ulong renderModelId, ulong handle)
        {
            OfferedRenderModel offered = new OfferedRenderModel(renderModelId);
            ulong[] pathHandles = GetRenderModelSubactionPaths(handle);
            if (pathHandles.Length > 0)
            {
                offered.UserPathHandles = pathHandles;
                offered.PoseUserPathHandle = GetRenderModelPoseUserPath(handle, pathHandles);
            }

            return offered;
        }

        // PathToString is not documented as thread-safe, so main thread only.
        private static void ResolveUserPaths(OfferedRenderModel offered)
        {
            string[] paths = new string[offered.UserPathHandles.Length];
            for (int i = 0; i < paths.Length; ++i)
            {
                paths[i] = PathToString(offered.UserPathHandles[i]);
            }

            offered.UserPaths = paths;
            if (offered.PoseUserPathHandle != XR_NULL_PATH)
            {
                offered.PoseUserPath = PathToString(offered.PoseUserPathHandle);
            }
        }

        private bool CanCreateRenderModel(ulong renderModelId)
        {
            if (!IsAvailable)
            {
                Debug.LogError(LogPrefix + "Render models are not available: no OpenXR session or the runtime does not support the render model extensions.");
                return false;
            }

            if (!_sessionRunning)
            {
                Debug.LogWarning(LogPrefix + "Render models cannot be created while the OpenXR session is not running.");
                return false;
            }

            if (renderModelId == 0)
            {
                Debug.LogError(LogPrefix + "A render model id of 0 is not valid.");
                return false;
            }

            return true;
        }

        private RenderModelCreation NewRenderModelCreation(ulong renderModelId, IList<string> supportedGltfExtensions)
        {
            // Described before its profile was bound. Ask again.
            _offeredRenderModels.TryGetValue(renderModelId, out OfferedRenderModel offered);
            bool describe = offered == null || offered.UserPathHandles.Length == 0;
            return new RenderModelCreation(this, renderModelId, CopyGltfExtensions(supportedGltfExtensions), describe, offered);
        }

        private RenderModel CompleteRenderModelCreation(RenderModelCreation creation)
        {
            if (!creation.CreateResult.IsSuccess())
            {
                if (creation.CreateResult == XrResult.SessionNotRunning && creation.Generation == _sessionGeneration)
                {
                    // The runtime stopped the session ahead of OnSessionEnd.
                    _sessionRunning = false;
                }

                LogCreateRenderModelFailure(creation.CreateResult);
                return null;
            }

            // CheckResult logged the call that failed.
            if (!creation.Succeeded)
            {
                return null;
            }

            OfferedRenderModel offered = creation.Description;
            if (creation.Describe)
            {
                ResolveUserPaths(offered);
                if (offered.UserPathHandles.Length > 0 && _offeredRenderModels.ContainsKey(creation.RenderModelId))
                {
                    _offeredRenderModels[creation.RenderModelId] = offered;
                }
            }

            RenderModel model = new RenderModel
            {
                Id = creation.RenderModelId,
                Handle = creation.Handle,
                Space = creation.Space,
                UserPaths = offered.UserPaths,
                PoseUserPath = offered.PoseUserPath,
                CacheId = ToGuid(creation.CacheId),
                GlbData = creation.GlbData,
                AnimatableNodeNames = creation.AnimatableNodeNames
            };
            _renderModels.Add(model);
            return model;
        }

        private static void LogCreateRenderModelFailure(XrResult result)
        {
            if (result == XrResult.SessionNotRunning)
            {
                Debug.LogWarning(LogPrefix + "Render models cannot be created while the OpenXR session is not running.");
            }
            else if (result == XrResult.RenderModelGltfExtensionRequiredEXT)
            {
                Debug.LogWarning(LogPrefix + "The render model requires a glTF extension that was not listed as supported.");
            }
            else
            {
                CheckResult(result, "xrCreateRenderModelEXT");
            }
        }

        /// <summary>
        /// Runs the work on a worker thread. Await from the main thread and call <see cref="TakeBackgroundResult"/> before using the result.
        /// </summary>
        private Task StartBackgroundWork(BackgroundWork work)
        {
            _backgroundWork.Add(work);
            work.Task = Task.Run(work.Execute);
            return work.Task;
        }

        /// <summary>Takes finished work off the pending list. False when the session was destroyed first and the result already released.</summary>
        private bool TakeBackgroundResult(BackgroundWork work)
        {
            _backgroundWork.Remove(work);
            return !work.Abandoned;
        }

        /// <summary>Waits for all worker work and abandons it. With <paramref name="releaseHandles"/> its handles are destroyed.</summary>
        private void AbandonBackgroundWork(bool releaseHandles)
        {
            foreach (BackgroundWork work in _backgroundWork)
            {
                work.Task.Wait();
                work.Abandoned = true;
                if (releaseHandles)
                {
                    work.ReleaseHandles();
                }
            }

            _backgroundWork.Clear();
            _refresh = null;
        }

        private ulong GetRenderModelPoseUserPath(ulong renderModel, ulong[] candidatePaths)
        {
            GCHandle hPaths = GCHandle.Alloc(candidatePaths, GCHandleType.Pinned);
            try
            {
                XrInteractionRenderModelTopLevelUserPathGetInfoEXT getInfo = new XrInteractionRenderModelTopLevelUserPathGetInfoEXT
                {
                    type = XrStructureType.InteractionRenderModelTopLevelUserPathGetInfoEXT,
                    topLevelUserPathCount = (uint)candidatePaths.Length,
                    topLevelUserPaths = hPaths.AddrOfPinnedObject()
                };

                if (!CheckResult(_xrGetRenderModelPoseTopLevelUserPathEXT(renderModel, ref getInfo, out ulong path), "xrGetRenderModelPoseTopLevelUserPathEXT"))
                {
                    return XR_NULL_PATH;
                }

                return path;
            }
            finally
            {
                hPaths.Free();
            }
        }

        private XrResult EnumerateRenderModelIds(ulong session, out ulong[] ids)
        {
            XrInteractionRenderModelIdsEnumerateInfoEXT enumerateInfo = new XrInteractionRenderModelIdsEnumerateInfoEXT
            {
                type = XrStructureType.InteractionRenderModelIdsEnumerateInfoEXT
            };
            ulong[] buffer = new ulong[InitialIdCapacity];
            XrResult result = _xrEnumerateInteractionRenderModelIdsEXT(session, ref enumerateInfo, (uint)buffer.Length, out uint idCount, buffer);
            if (result == XrResult.SizeInsufficient)
            {
                buffer = new ulong[idCount];
                result = _xrEnumerateInteractionRenderModelIdsEXT(session, ref enumerateInfo, idCount, out idCount, buffer);
            }

            ids = new ulong[0];
            if (result.IsSuccess())
            {
                Array.Resize(ref buffer, (int)idCount);
                ids = buffer;
            }

            return result;
        }

        private ulong[] GetRenderModelSubactionPaths(ulong renderModel)
        {
            XrInteractionRenderModelSubactionPathInfoEXT pathInfo = new XrInteractionRenderModelSubactionPathInfoEXT
            {
                type = XrStructureType.InteractionRenderModelSubactionPathInfoEXT
            };
            ulong[] paths = new ulong[InitialPathCapacity];
            XrResult result = _xrEnumerateRenderModelSubactionPathsEXT(renderModel, ref pathInfo, (uint)paths.Length, out uint pathCount, paths);
            if (result == XrResult.SizeInsufficient)
            {
                paths = new ulong[pathCount];
                result = _xrEnumerateRenderModelSubactionPathsEXT(renderModel, ref pathInfo, pathCount, out pathCount, paths);
            }

            if (!CheckResult(result, "xrEnumerateRenderModelSubactionPathsEXT"))
            {
                return new ulong[0];
            }

            Array.Resize(ref paths, (int)pathCount);
            return paths;
        }

        /// <param name="gltfExtensions">A block from <see cref="AllocUtf8StringArray"/>.</param>
        private XrResult CreateRenderModelHandle(ulong session, ulong renderModelId, IntPtr gltfExtensions, uint gltfExtensionCount, out ulong renderModel)
        {
            XrRenderModelCreateInfoEXT createInfo = new XrRenderModelCreateInfoEXT
            {
                type = XrStructureType.RenderModelCreateInfoEXT,
                renderModelId = renderModelId,
                gltfExtensionCount = gltfExtensionCount,
                gltfExtensions = gltfExtensions
            };

            XrResult result = _xrCreateRenderModelEXT(session, ref createInfo, out renderModel);
            if (!result.IsSuccess())
            {
                renderModel = 0;
            }

            return result;
        }

        private bool CreateRenderModelSpace(ulong session, ulong renderModel, out ulong space)
        {
            XrRenderModelSpaceCreateInfoEXT createInfo = new XrRenderModelSpaceCreateInfoEXT
            {
                type = XrStructureType.RenderModelSpaceCreateInfoEXT,
                renderModel = renderModel
            };

            if (!CheckResult(_xrCreateRenderModelSpaceEXT(session, ref createInfo, out space), "xrCreateRenderModelSpaceEXT"))
            {
                space = 0;
                return false;
            }

            return true;
        }

        private bool GetRenderModelProperties(ulong renderModel, out XrUuid cacheId, out uint animatableNodeCount)
        {
            XrRenderModelPropertiesGetInfoEXT getInfo = new XrRenderModelPropertiesGetInfoEXT
            {
                type = XrStructureType.RenderModelPropertiesGetInfoEXT
            };
            XrRenderModelPropertiesEXT properties = new XrRenderModelPropertiesEXT { type = XrStructureType.RenderModelPropertiesEXT };

            if (!CheckResult(_xrGetRenderModelPropertiesEXT(renderModel, ref getInfo, ref properties), "xrGetRenderModelPropertiesEXT"))
            {
                cacheId = default;
                animatableNodeCount = 0;
                return false;
            }

            cacheId = properties.cacheId;
            animatableNodeCount = properties.animatableNodeCount;
            return true;
        }

        private bool CreateRenderModelAsset(ulong session, XrUuid cacheId, out ulong asset)
        {
            XrRenderModelAssetCreateInfoEXT createInfo = new XrRenderModelAssetCreateInfoEXT
            {
                type = XrStructureType.RenderModelAssetCreateInfoEXT,
                cacheId = cacheId
            };

            if (!CheckResult(_xrCreateRenderModelAssetEXT(session, ref createInfo, out asset), "xrCreateRenderModelAssetEXT"))
            {
                asset = 0;
                return false;
            }

            return true;
        }

        private bool GetRenderModelAssetData(ulong asset, out byte[] glbData)
        {
            glbData = null;

            XrRenderModelAssetDataGetInfoEXT getInfo = new XrRenderModelAssetDataGetInfoEXT
            {
                type = XrStructureType.RenderModelAssetDataGetInfoEXT
            };
            XrRenderModelAssetDataEXT assetData = new XrRenderModelAssetDataEXT
            {
                type = XrStructureType.RenderModelAssetDataEXT,
                bufferCapacityInput = 0,
                buffer = IntPtr.Zero
            };

            if (!CheckResult(_xrGetRenderModelAssetDataEXT(asset, ref getInfo, ref assetData), "xrGetRenderModelAssetDataEXT"))
            {
                return false;
            }

            if (assetData.bufferCountOutput == 0)
            {
                Debug.LogError(LogPrefix + "The runtime returned an empty render model asset.");
                return false;
            }

            byte[] data = new byte[assetData.bufferCountOutput];
            GCHandle hData = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                assetData.bufferCapacityInput = (uint)data.Length;
                assetData.buffer = hData.AddrOfPinnedObject();

                if (!CheckResult(_xrGetRenderModelAssetDataEXT(asset, ref getInfo, ref assetData), "xrGetRenderModelAssetDataEXT"))
                {
                    return false;
                }
            }
            finally
            {
                hData.Free();
            }

            glbData = data;
            return true;
        }

        private bool GetAnimatableNodeNames(ulong asset, uint nodeCount, out List<string> names)
        {
            names = new List<string>((int)nodeCount);
            if (nodeCount == 0)
            {
                return true;
            }

            int bufferSize = (int)nodeCount * XrRenderModelAssetNodePropertiesEXT.Size;
            IntPtr nodeProperties = Marshal.AllocHGlobal(bufferSize);
            try
            {
                byte[] zero = new byte[bufferSize];
                Marshal.Copy(zero, 0, nodeProperties, bufferSize);

                XrRenderModelAssetPropertiesGetInfoEXT getInfo = new XrRenderModelAssetPropertiesGetInfoEXT
                {
                    type = XrStructureType.RenderModelAssetPropertiesGetInfoEXT
                };
                XrRenderModelAssetPropertiesEXT assetProperties = new XrRenderModelAssetPropertiesEXT
                {
                    type = XrStructureType.RenderModelAssetPropertiesEXT,
                    nodePropertyCount = nodeCount,
                    nodeProperties = nodeProperties
                };

                if (!CheckResult(_xrGetRenderModelAssetPropertiesEXT(asset, ref getInfo, ref assetProperties), "xrGetRenderModelAssetPropertiesEXT"))
                {
                    names = null;
                    return false;
                }

                for (int i = 0; i < nodeCount; ++i)
                {
                    names.Add(XrRenderModelAssetNodePropertiesEXT.ReadUniqueName(nodeProperties + i * XrRenderModelAssetNodePropertiesEXT.Size));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(nodeProperties);
            }

            return true;
        }

        /// <summary>
        /// The frame's predicted display time from <see cref="ValveOpenXRDisplayTimeFeature"/>, fixed per frame. False before the first frame.
        /// </summary>
        private bool TryGetDisplayTime(out long displayTime)
        {
            int frame = Time.frameCount;
            if (frame == _displayTimeFrame)
            {
                displayTime = _displayTime;
                return true;
            }

            if (!ValveOpenXRDisplayTimeFeature.TryGetPredictedDisplayTime(out displayTime))
            {
                return false;
            }

            _displayTimeFrame = frame;
            _displayTime = displayTime;
            return true;
        }

        private ulong GetPath(string userPath)
        {
            if (!_pathCache.TryGetValue(userPath, out ulong path))
            {
                path = StringToPath(userPath);
                if (path != XR_NULL_PATH)
                {
                    _pathCache[userPath] = path;
                }
            }

            return path;
        }

        /// <summary>
        /// Encodes the strings as one const char* const* block. Null entries are skipped, an empty list gives IntPtr.Zero. Free with Marshal.FreeHGlobal.
        /// </summary>
        private static IntPtr AllocUtf8StringArray(string[] values, out uint count)
        {
            count = 0;
            int textSize = 0;
            byte[][] encoded = new byte[values.Length][];
            for (int i = 0; i < values.Length; ++i)
            {
                if (values[i] != null)
                {
                    encoded[i] = System.Text.Encoding.UTF8.GetBytes(values[i]);
                    textSize += encoded[i].Length + 1;
                    ++count;
                }
            }

            if (count == 0)
            {
                return IntPtr.Zero;
            }

            int tableSize = (int)count * IntPtr.Size;
            IntPtr block = Marshal.AllocHGlobal(tableSize + textSize);
            IntPtr text = block + tableSize;
            int slot = 0;
            foreach (byte[] bytes in encoded)
            {
                if (bytes == null)
                {
                    continue;
                }

                Marshal.WriteIntPtr(block, slot * IntPtr.Size, text);
                Marshal.Copy(bytes, 0, text, bytes.Length);
                Marshal.WriteByte(text, bytes.Length, 0);
                text += bytes.Length + 1;
                ++slot;
            }

            return block;
        }

        private static bool CheckResult(XrResult xrResult, string functionName)
        {
            if (xrResult.IsSuccess())
            {
                return true;
            }

            Debug.LogError(LogPrefix + $"{functionName} failed with XrResult {xrResult}.");
            return false;
        }

        // For per-frame calls. Logs the first failure only.
        private static bool CheckFrameResult(XrResult xrResult, string functionName, ref bool failureLogged)
        {
            if (xrResult.IsSuccess())
            {
                failureLogged = false;
                return true;
            }

            if (!failureLogged)
            {
                failureLogged = true;
                Debug.LogError(LogPrefix + $"{functionName} failed with XrResult {xrResult}. Repeated failures are not logged.");
            }
            return false;
        }

        #endregion

        #region Background work

        /// <summary>
        /// Runtime calls made on a worker thread. They use only the feature's function pointers and their own handles, which stay valid until
        /// <see cref="AbandonBackgroundWork"/>.
        /// </summary>
        private abstract class BackgroundWork
        {
            protected readonly ValveOpenXRRenderModelFeature Feature;
            protected readonly ulong Session;
            internal readonly int Generation;
            internal Task Task;
            internal bool Abandoned;

            protected BackgroundWork(ValveOpenXRRenderModelFeature feature)
            {
                Feature = feature;
                Session = feature._xrSession;
                Generation = feature._sessionGeneration;
            }

            internal void Execute()
            {
                // Nothing may escape the worker thread.
                try
                {
                    Run();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            protected abstract void Run();

            // Main thread, when the session goes away before the result was taken.
            internal virtual void ReleaseHandles()
            {
            }
        }

        private sealed class OfferedRenderModelsRefresh : BackgroundWork
        {
            internal readonly bool DescribeAll;
            internal XrResult Result = XrResult.RuntimeFailure;
            internal ulong[] Ids = new ulong[0];
            // Models described by this refresh, without their path strings.
            internal readonly Dictionary<ulong, OfferedRenderModel> Descriptions = new Dictionary<ulong, OfferedRenderModel>();
            private readonly string[] _gltfExtensions;
            private readonly List<ulong> _describedIds;

            internal OfferedRenderModelsRefresh(ValveOpenXRRenderModelFeature feature, string[] gltfExtensions, List<ulong> describedIds, bool describeAll)
                : base(feature)
            {
                _gltfExtensions = gltfExtensions;
                _describedIds = describedIds;
                DescribeAll = describeAll;
            }

            protected override void Run()
            {
                XrResult result = Feature.EnumerateRenderModelIds(Session, out ulong[] ids);
                if (!result.IsSuccess())
                {
                    Result = result;
                    return;
                }

                // Paths need a handle, created and destroyed per id.
                IntPtr extensions = AllocUtf8StringArray(_gltfExtensions, out uint extensionCount);
                try
                {
                    foreach (ulong id in ids)
                    {
                        if (_describedIds.Contains(id))
                        {
                            continue;
                        }

                        OfferedRenderModel offered = new OfferedRenderModel(id);
                        result = Feature.CreateRenderModelHandle(Session, id, extensions, extensionCount, out ulong handle);
                        if (result == XrResult.SessionNotRunning)
                        {
                            Result = result;
                            return;
                        }

                        if (result.IsSuccess())
                        {
                            try
                            {
                                offered = Feature.DescribeRenderModel(id, handle);
                            }
                            finally
                            {
                                CheckResult(Feature._xrDestroyRenderModelEXT(handle), "xrDestroyRenderModelEXT");
                            }
                        }
                        else
                        {
                            LogCreateRenderModelFailure(result);
                        }

                        Descriptions[id] = offered;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(extensions);
                }

                Ids = ids;
                Result = XrResult.Success;
            }
        }

        private sealed class RenderModelCreation : BackgroundWork
        {
            internal readonly ulong RenderModelId;
            internal readonly bool Describe;
            internal XrResult CreateResult = XrResult.RuntimeFailure;
            internal bool Succeeded;
            internal ulong Handle;
            internal ulong Space;
            // Cached, or freshly described without path strings.
            internal OfferedRenderModel Description;
            internal XrUuid CacheId;
            internal byte[] GlbData;
            internal List<string> AnimatableNodeNames;
            private readonly string[] _gltfExtensions;

            internal RenderModelCreation(ValveOpenXRRenderModelFeature feature, ulong renderModelId, string[] gltfExtensions, bool describe,
                OfferedRenderModel cached) : base(feature)
            {
                RenderModelId = renderModelId;
                _gltfExtensions = gltfExtensions;
                Describe = describe;
                Description = cached;
            }

            protected override void Run()
            {
                IntPtr extensions = AllocUtf8StringArray(_gltfExtensions, out uint extensionCount);
                try
                {
                    CreateResult = Feature.CreateRenderModelHandle(Session, RenderModelId, extensions, extensionCount, out Handle);
                }
                finally
                {
                    Marshal.FreeHGlobal(extensions);
                }

                if (!CreateResult.IsSuccess())
                {
                    return;
                }

                ulong asset = 0;
                try
                {
                    if (Describe)
                    {
                        Description = Feature.DescribeRenderModel(RenderModelId, Handle);
                    }

                    Succeeded = Feature.GetRenderModelProperties(Handle, out CacheId, out uint animatableNodeCount) &&
                                Feature.CreateRenderModelAsset(Session, CacheId, out asset) &&
                                Feature.GetRenderModelAssetData(asset, out GlbData) &&
                                Feature.GetAnimatableNodeNames(asset, animatableNodeCount, out AnimatableNodeNames) &&
                                Feature.CreateRenderModelSpace(Session, Handle, out Space);
                }
                finally
                {
                    if (asset != 0)
                    {
                        CheckResult(Feature._xrDestroyRenderModelAssetEXT(asset), "xrDestroyRenderModelAssetEXT");
                    }

                    if (!Succeeded)
                    {
                        ReleaseHandles();
                    }
                }
            }

            internal override void ReleaseHandles()
            {
                if (Space != 0)
                {
                    CheckResult(Feature._xrDestroySpace(Space), "xrDestroySpace");
                    Space = 0;
                }

                if (Handle != 0)
                {
                    CheckResult(Feature._xrDestroyRenderModelEXT(Handle), "xrDestroyRenderModelEXT");
                    Handle = 0;
                }
            }
        }

        #endregion
    }
}
