using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Publishes the app's player locomotion delta ("appSpaceDeltaPose") and the camera's depth range to the runtime
    /// every frame via the XR_VALVE_app_space_delta_pose extension, so Steam Frame's frame synthesis can account for
    /// smooth locomotion, turning and vehicles without the app implementing space warp itself.
    ///
    /// Call <see cref="SetTrackingSpace"/> with the transform your locomotion code moves (the parent of the tracked
    /// camera), or add <see cref="AppSpaceDeltaPoseTrackingSpace"/> to it. Head tracking must not be part of it.
    /// Call <see cref="MarkDiscontinuity"/> in any frame where the player jumps instead of moving (teleport, station
    /// snap, world load, recenter); that frame's delta is identity, which turns locomotion interpolation off for it.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: App Space Delta Pose",
        Desc = "Reports player locomotion (smooth movement, turning, vehicles) and the camera depth range to the runtime each frame so Steam Frame's frame synthesis can account for them.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#app-space-delta-pose",
        OpenxrExtensionStrings = ExtensionName,
        Version = "1",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        FeatureId = featureId)]
#endif
    public class ValveOpenXRAppSpaceDeltaPoseFeature : OpenXRFeature
    {
        public const string featureId = "com.valvesoftware.openxr.utils.app_space_delta_pose";
        public const string ExtensionName = "XR_VALVE_app_space_delta_pose";

        public static ValveOpenXRAppSpaceDeltaPoseFeature Instance => OpenXRSettings.Instance?.GetFeature<ValveOpenXRAppSpaceDeltaPoseFeature>();

        /// <summary>
        /// Transform mapping tracking space to world; the change in it between frames is what gets published. The
        /// camera supplies the near/far clip range; if null, Camera.main is used.
        /// </summary>
        public void SetTrackingSpace(Transform trackingSpace, Camera camera = null)
        {
            _trackingSpace = trackingSpace;
            _camera = camera;
            MarkDiscontinuity();
        }

        /// <summary>
        /// Publish identity for this frame instead of the measured delta: the player jumped (teleport, station snap,
        /// world load, recenter) and the motion must not be interpolated. Call it in the same frame as the jump.
        /// </summary>
        public void MarkDiscontinuity()
        {
            _havePrevious = false;
        }

        private ulong _xrSession;
        private ulong _xrAppSpace;
        private PFN_xrSetAppSpaceDeltaPoseVALVE _xrSetAppSpaceDeltaPoseVALVE;

        private Transform _trackingSpace;
        private Camera _camera;
        private bool _havePrevious;
        private Vector3 _prevPosition;
        private Quaternion _prevRotation;

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            return OpenXRRuntime.IsExtensionEnabled(ExtensionName);
        }

        protected override void OnSessionCreate(ulong xrSession)
        {
            _xrSession = xrSession;
            _xrSetAppSpaceDeltaPoseVALVE = ValveOpenXRSupportFeature.GetOpenXrInstanceProc<PFN_xrSetAppSpaceDeltaPoseVALVE>("xrSetAppSpaceDeltaPoseVALVE");
            if (_xrSetAppSpaceDeltaPoseVALVE == null)
            {
                Debug.LogError($"[{ExtensionName}] xrSetAppSpaceDeltaPoseVALVE not available; nothing will be published.");
                return;
            }

            Debug.Log($"[{ExtensionName}] ready; publishing starts once SetTrackingSpace() is called (tracking space {(_trackingSpace != null ? "already set" : "not set yet")}).");
            Application.onBeforeRender += OnBeforeRender;
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            Application.onBeforeRender -= OnBeforeRender;
            _xrSetAppSpaceDeltaPoseVALVE = null;
            _xrSession = _xrAppSpace = 0;
        }

        protected override void OnAppSpaceChange(ulong xrSpace)
        {
            _xrAppSpace = xrSpace;
            MarkDiscontinuity();
        }

        private void OnBeforeRender()
        {
            if (_trackingSpace == null || _xrSetAppSpaceDeltaPoseVALVE == null || _xrAppSpace == 0)
                return;

            Vector3 position = _trackingSpace.position;
            Quaternion rotation = _trackingSpace.rotation;
            Vector3 scale = _trackingSpace.lossyScale; // world units per tracking-space meter

            // The transform the app applied to its tracking space this frame, inverse(T_prev) * T_cur: the current tracking
            // space expressed in the previous one, in tracking-space meters. Same convention as XR_FB_space_warp and
            // XR_EXT_frame_synthesis.
            Quaternion deltaRotation = Quaternion.identity;
            Vector3 deltaPosition = Vector3.zero;
            if (_havePrevious)
            {
                Quaternion invPrevRotation = Quaternion.Inverse(_prevRotation);
                deltaRotation = invPrevRotation * rotation;
                deltaPosition = invPrevRotation * (position - _prevPosition);
                deltaPosition = new Vector3(deltaPosition.x / scale.x, deltaPosition.y / scale.y, deltaPosition.z / scale.z);
            }
            _prevPosition = position;
            _prevRotation = rotation;
            _havePrevious = true;

            // Clip planes in tracking-space meters. Reversed Z is expressed by swapping near and far, as in
            // XrCompositionLayerDepthInfoKHR. 0/0 means unknown.
            float nearZ = 0.0f, farZ = 0.0f;
            Camera camera = _camera != null ? _camera : Camera.main;
            if (camera != null)
            {
                nearZ = camera.nearClipPlane / scale.x;
                farZ = camera.farClipPlane / scale.x;
                if (SystemInfo.usesReversedZBuffer)
                    (nearZ, farZ) = (farZ, nearZ);
            }

            var setInfo = new XrAppSpaceDeltaPoseSetInfoVALVE
            {
                type = XrStructureType.XR_TYPE_APP_SPACE_DELTA_POSE_SET_INFO_VALVE,
                // Unity hides the frame loop, so let the runtime attach this to the frame it most recently returned from xrWaitFrame.
                flags = XrAppSpaceDeltaPoseSetFlagsVALVE.XR_APP_SPACE_DELTA_POSE_SET_LAST_WAIT_FRAME_BIT_VALVE,
                appSpace = _xrAppSpace,
                // Unity (left-handed) -> OpenXR (right-handed): flip z, negate x/y of the quaternion.
                orientation = new Vector4(-deltaRotation.x, -deltaRotation.y, deltaRotation.z, deltaRotation.w),
                position = new Vector3(deltaPosition.x, deltaPosition.y, -deltaPosition.z),
                nearZ = nearZ,
                farZ = farZ,
            };

            if (_xrSetAppSpaceDeltaPoseVALVE(_xrSession, ref setInfo) != 0)
            {
                Debug.LogError($"[{ExtensionName}] xrSetAppSpaceDeltaPoseVALVE failed; disabling");
                Application.onBeforeRender -= OnBeforeRender;
            }
        }

#if UNITY_EDITOR
        protected override void OnEnabledChange()
        {
            if (enabled)
                OpenXRSettings.ActiveBuildTargetInstance.GetFeature<ValveOpenXRSupportFeature>().enabled = true;
        }

        protected override void GetValidationChecks(List<ValidationRule> results, BuildTargetGroup target)
        {
            results.Add(new ValidationRule(this)
            {
                message = "Valve Utils rendering feature must be enabled.",
                error = true,
                checkPredicate = () => OpenXRSettings.ActiveBuildTargetInstance.GetFeature<ValveOpenXRSupportFeature>().enabled,
                fixIt = () => OpenXRSettings.ActiveBuildTargetInstance.GetFeature<ValveOpenXRSupportFeature>().enabled = true,
                fixItAutomatic = true,
                fixItMessage = "Enable Valve Utils rendering feature"
            });
        }
#endif

        private enum XrStructureType : uint
        {
            XR_TYPE_APP_SPACE_DELTA_POSE_SET_INFO_VALVE = 1000084000,
        }

        [Flags]
        private enum XrAppSpaceDeltaPoseSetFlagsVALVE : ulong
        {
            XR_APP_SPACE_DELTA_POSE_SET_LAST_WAIT_FRAME_BIT_VALVE = 0x00000001,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrAppSpaceDeltaPoseSetInfoVALVE
        {
            public XrStructureType type;
            public IntPtr next;
            public XrAppSpaceDeltaPoseSetFlagsVALVE flags;
            public ulong appSpace;
            public long displayTime;
            public Vector4 orientation; // XrPosef appSpaceDeltaPose: quaternion xyzw, then position
            public Vector3 position;
            public float nearZ;
            public float farZ;
        }

        private delegate int PFN_xrSetAppSpaceDeltaPoseVALVE(ulong session, ref XrAppSpaceDeltaPoseSetInfoVALVE setInfo);
    }
}
