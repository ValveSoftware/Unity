using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
using Object = UnityEngine.Object;

namespace Valve.OpenXR.Utils
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: Settings for Unity's Foveated Rendering",
        Desc = "Feature extension showing how to work with OpenXR foveated rendering.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        OpenxrExtensionStrings = extensionStrings,
        Version = "0.1.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        FeatureId = featureId
    )]
#endif
    public class ValveOpenXRFoveatedRenderingFeature : OpenXRFeature
    {
        public const string featureId = "com.valvesoftware.openxr.utils.foveated_rendering";
        private const string extensionStrings =
            "XR_FB_foveation " + 
            "XR_FB_foveation_configuration " +
            "XR_FB_foveation_vulkan " +
            "XR_FB_swapchain_update_state " +
            "XR_META_foveation_eye_tracked " +
            "XR_META_vulkan_swapchain_create_info";

        [SerializeField]
        private bool applySettingsOnStartup;

        [SerializeField]
        [Range(0, 1)]
        public float initialFoveationLevel;

        [SerializeField]
        public bool initialUseEyeTracking;

        private delegate XrResult XrGetFoveationEyeTrackedStateMETADelegate (ulong session, ref XrFoveationEyeTrackedStateMETA foveationState);

        private XrGetFoveationEyeTrackedStateMETADelegate _xrGetFoveationEyeTrackedStateMETA;
        private bool _eyeTrackedStateResolved;

        private ulong _xrInstance;
        private ulong _xrSession;

#if USE_XR_DISPLAY_FOVEATION_API
        private static readonly List<XRDisplaySubsystem> s_displays = new List<XRDisplaySubsystem>();
        private static XRDisplaySubsystem GetRunningDisplay()
        {
            SubsystemManager.GetSubsystems(s_displays);
            foreach (XRDisplaySubsystem display in s_displays)
            {
                if (display.running)
                {
                    return display;
                }
            }

            return null;
        }
#endif

        public float foveatedRenderingLevel
        {
            get
            {
                float level = 0f;

#if USE_XR_DISPLAY_FOVEATION_API
                XRDisplaySubsystem display = GetRunningDisplay();
                if (display != null)
                {
                    level = display.foveatedRenderingLevel;
                }
#else
                NativeMethods.FBGetFoveationLevel(out var discreteLevel);
                switch ((FoveatedRenderingLevel)discreteLevel)
                { 
                    case FoveatedRenderingLevel.HighTop: // fallthrough
                    case FoveatedRenderingLevel.High: level = 1.0f; break;
                    case FoveatedRenderingLevel.Medium: level = 0.75f; break;
                    case FoveatedRenderingLevel.Low: level = 0.25f; break;
                    case FoveatedRenderingLevel.Off:
                    default: level = 0f; break;
                }
#endif
                return level;
            }
            set
            {
#if USE_XR_DISPLAY_FOVEATION_API
                XRDisplaySubsystem display = GetRunningDisplay();
                if (display != null)
                {
                    display.foveatedRenderingLevel = value;
                }
#else
                var level = value switch
                {
                    > 0.75f => (uint)FoveatedRenderingLevel.High,
                    > 0.25f => (uint)FoveatedRenderingLevel.Medium,
                    > 0.0001f => (uint)FoveatedRenderingLevel.Low,
                    _ => (uint)FoveatedRenderingLevel.Off
                };

                NativeMethods.FBGetFoveationDynamic(out var dynamic);
                NativeMethods.FBSetFoveationLevel(_xrSession, level, 0.0f, dynamic);
#endif
            }
        }

        public bool eyeTrackedFoveation
        {
            get
            {
                bool isEyeTracked = false;
#if USE_XR_DISPLAY_FOVEATION_API
                XRDisplaySubsystem display = GetRunningDisplay();
                if (display != null)
                {
                    isEyeTracked = display.foveatedRenderingFlags == XRDisplaySubsystem.FoveatedRenderingFlags.GazeAllowed;
                }
#else
                NativeMethods.MetaGetFoveationEyeTracked(out isEyeTracked);
#endif
                return isEyeTracked;
            }
            set
            {
#if USE_XR_DISPLAY_FOVEATION_API
                SubsystemManager.GetSubsystems(s_displays);
                foreach (XRDisplaySubsystem display in s_displays)
                {
                    display.foveatedRenderingFlags = value
                        ? XRDisplaySubsystem.FoveatedRenderingFlags.GazeAllowed 
                        : XRDisplaySubsystem.FoveatedRenderingFlags.None;
                }
#else
                NativeMethods.MetaSetFoveationEyeTracked(_xrSession, value);
#endif
            }
        }

        public bool GetFoveationEyeTrackedCenter(ref Vector2 leftEye, ref Vector2 rightEye)
        {
            if (_xrInstance == 0 || _xrSession == 0)
            {
                return false;
            }

            if (!_eyeTrackedStateResolved)
            {
                _eyeTrackedStateResolved = true;
                ValveOpenXRDelegates.TryGetInstanceProc(xrGetInstanceProcAddr, _xrInstance, "xrGetFoveationEyeTrackedStateMETA",
                    out _xrGetFoveationEyeTrackedStateMETA);
            }

            if (_xrGetFoveationEyeTrackedStateMETA == null)
            {
                return false;
            }

            XrFoveationEyeTrackedStateMETA eyeTrackedState = new XrFoveationEyeTrackedStateMETA { type = XrStructureType.FoveationEyeTrackedStateMETA };
            XrResult result = _xrGetFoveationEyeTrackedStateMETA(_xrSession, ref eyeTrackedState);

            if (result != XrResult.Success || eyeTrackedState.flags != XrFoveationEyeTrackedStateFlagsMETA.XR_FOVEATION_EYE_TRACKED_STATE_VALID_BIT_META)
            {
                return false;
            }

            leftEye.Set(eyeTrackedState.leftFoveationCenter.X, eyeTrackedState.leftFoveationCenter.Y);
            rightEye.Set(eyeTrackedState.rightFoveationCenter.X, eyeTrackedState.rightFoveationCenter.Y);
            return true;
        }

#if UNITY_EDITOR
        protected override void GetValidationChecks(List<OpenXRFeature.ValidationRule> rules, BuildTargetGroup targetGroup)
        {
            base.GetValidationChecks(rules, targetGroup);

            rules.Add(new ValidationRule(this)
            {
                message = "This feature is only supported on Vulkan graphics API.",
                error = true,
                checkPredicate = () =>
                {
                    if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android))
                    {
                        GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                        if (apis.Length >= 1 && apis[0] == GraphicsDeviceType.Vulkan)
                        {
                            return true;
                        }
                        return false;
                    }
                    return true;
                },
                fixIt = () =>
                {
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
                },
                fixItAutomatic = true,
                fixItMessage = "Set Vulkan as Graphics API"
            });

#if UNITY_2023_2_OR_NEWER
            rules.Add(new ValidationRule(this)
            {
                message = "This feature requires the foveated rendering API set to SRP foveation.",
                error = true,
                checkPredicate = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    if (settings)
                    {
                        return settings.foveatedRenderingApi == OpenXRSettings.BackendFovationApi.SRPFoveation;
                    }
                    return true;
                },
                fixIt = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    settings.foveatedRenderingApi = OpenXRSettings.BackendFovationApi.SRPFoveation;
                },
                fixItAutomatic = true,
                fixItMessage = "Set SPR foveation as the foveated rendering API."
            });
#endif
#if UNITY_6000_0_OR_NEWER
            rules.Add(new ValidationRule(this)
            {
                message = "Unity Foveated Rendering feature must be enabled.",
                error = true,
                checkPredicate = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    if (settings == null)
                        return false;

                    var foveationFeature = settings.GetFeature<FoveatedRenderingFeature>();
                    return foveationFeature.enabled;
                },
                fixIt = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    if (settings != null)
                    {
                        var foveationFeature = settings.GetFeature<FoveatedRenderingFeature>();
                        foveationFeature.enabled = true;
                    }
                },
                fixItAutomatic = true,
                fixItMessage = "Enable Unity's Foveated Rendering feature"
            });
#endif
#if UNITY_6000_3_OR_NEWER
            rules.Add(new ValidationRule(this)
            {
                message = "Optimize Buffer Discards must be enabled.",
                error = true,
                checkPredicate = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    if (settings == null)
                        return false;

                    var supportFeature = settings.GetFeature<ValveOpenXRSupportFeature>();
                    return supportFeature.enabled && supportFeature.optimizeBufferDiscards;
                },
                fixIt = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    if (settings != null)
                    {
                        var supportFeature = settings.GetFeature<ValveOpenXRSupportFeature>();
                        supportFeature.enabled = true;
                        supportFeature.optimizeBufferDiscards = true;
                    }
                },
                fixItAutomatic = true,
                fixItMessage = "Enable Optimize Buffer Discards"
            });
#endif
            rules.Add(new ValidationRule(this)
            {
                message = "Depth submission may cause issues with Foveated Rendering.",
                error = false,
                checkPredicate = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    if (settings == null)
                        return false;

                    return settings.depthSubmissionMode == OpenXRSettings.DepthSubmissionMode.None;
                },
                fixIt = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    if (settings != null)
                    {
                        settings.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
                    }
                },
                fixItAutomatic = true,
                fixItMessage = "Disable Depth Submission Mode"
            });

            rules.Add(new ValidationRule(this)
            {
                message = "Graphics Jobs may cause issues with Foveated Rendering.",
                error = false,
                checkPredicate = () =>
                {
                    return PlayerSettings.graphicsJobs == false;
                },
                fixIt = () =>
                {
                    PlayerSettings.graphicsJobs = false;
                },
                fixItAutomatic = true,
                fixItMessage = "Disable Graphics Jobs"
            });
        }
#endif

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            _xrInstance = xrInstance;
            return true;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            _xrInstance = 0;
            _xrGetFoveationEyeTrackedStateMETA = null;
            _eyeTrackedStateResolved = false;
        }

        protected override void OnSessionCreate(ulong xrSession)
        {
            _xrSession = xrSession;

            // Work around for Lepton not knowing about eye-tracking related permissions.
            NativeMethods.Internal_SetHasEyeTrackingPermissions(true);
        }

        protected override void OnSessionStateChange(int oldState, int newState)
        {
            if (oldState == (int)XrSessionState.Visible &&
                newState == (int)XrSessionState.Focused)
            {
                if (applySettingsOnStartup)
                {
                    foveatedRenderingLevel = initialFoveationLevel;
                    eyeTrackedFoveation = initialUseEyeTracking;
                }
            }
        }

        #region OpenXR Plugin DLL Imports and Dependencies

        internal static class NativeMethods
        {
            [DllImport("UnityOpenXR", EntryPoint = "FBSetFoveationLevel")]
            internal static extern void FBSetFoveationLevel(UInt64 session, UInt32 level, float verticalOffset, UInt32 dynamic);

            [DllImport("UnityOpenXR", EntryPoint = "FBGetFoveationLevel")]
            internal static extern void FBGetFoveationLevel(out UInt32 level);

            [DllImport("UnityOpenXR", EntryPoint = "FBGetFoveationDynamic")]
            internal static extern void FBGetFoveationDynamic(out UInt32 dynamic);

            [DllImport("UnityOpenXR", EntryPoint = "MetaSetFoveationEyeTracked")]
            internal static extern void MetaSetFoveationEyeTracked(UInt64 session, bool isEyeTracked);

            [DllImport("UnityOpenXR", EntryPoint = "MetaGetFoveationEyeTracked")]
            internal static extern void MetaGetFoveationEyeTracked(out bool isEyeTracked);

            [DllImport("UnityOpenXR", EntryPoint = "OculusFoveation_SetHasEyeTrackingPermissions")]
            internal static extern void Internal_SetHasEyeTrackingPermissions([MarshalAs(UnmanagedType.I1)] bool value);
        }

        // foveationCenter[XR_FOVEATION_CENTER_SIZE_META] flattened to avoid array marshalling.
        [StructLayout(LayoutKind.Sequential)]
        private struct XrFoveationEyeTrackedStateMETA
        {
            public XrStructureType type;
            public IntPtr next;
            public XrVector2f leftFoveationCenter;
            public XrVector2f rightFoveationCenter;
            public XrFoveationEyeTrackedStateFlagsMETA flags;
        }

        [Flags]
        private enum XrFoveationEyeTrackedStateFlagsMETA : ulong   // XrFlags64
        {
            XR_FOVEATION_EYE_TRACKED_STATE_VALID_BIT_META = 0x00000001
        }
        
        #endregion

#if UNITY_EDITOR
        internal class ValveOpenXRFoveatedRenderingFeatureEditorWindow : EditorWindow
        {
            private Object feature;
            private Editor featureEditor;

            public static EditorWindow Create(Object feature)
            {
                var window = EditorWindow.GetWindow<ValveOpenXRFoveatedRenderingFeatureEditorWindow>(true, "Valve OpenXR Foveated Rendering Configuration", true);
                window.feature = feature;
                window.featureEditor = Editor.CreateEditor(feature);
                return window;
            }

            private void OnGUI()
            {
                featureEditor.OnInspectorGUI();
            }
        }
#endif
    }

    internal enum FoveatedRenderingLevel
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        HighTop = 4
    }
}
