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

        private ulong _xrSession;

        public float foveatedRenderingLevel
        {
            get
            {
                float level = 0f;

#if UNITY_OPENXR_1_9_0
#if USE_XR_DISPLAY_FOVEATION_API
                List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
                SubsystemManager.GetSubsystems(xrDisplays);
                foreach (var display in xrDisplays)
                {
                    if (display.running)
                    {
                        level = display.foveatedRenderingLevel;
                        break;
                    }
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
#endif
                return level;
            }
            set
            {
#if UNITY_OPENXR_1_9_0
#if USE_XR_DISPLAY_FOVEATION_API
                List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
                SubsystemManager.GetSubsystems(xrDisplays);
                foreach (var display in xrDisplays)
                {
                    if (display.running)
                    {
                        display.foveatedRenderingLevel = value;
                        break;
                    }
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
#endif
            }
        }

        public bool eyeTrackedFoveation
        {
            get
            {
                bool isEyeTracked = false;
#if UNITY_OPENXR_1_9_0
#if USE_XR_DISPLAY_FOVEATION_API
                List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
                SubsystemManager.GetSubsystems(xrDisplays);
                foreach (var display in xrDisplays)
                {
                    if (display.running)
                    {
                        isEyeTracked = display.foveatedRenderingFlags == XRDisplaySubsystem.FoveatedRenderingFlags.GazeAllowed;
                        break;
                    }
                }
#else
                NativeMethods.MetaGetFoveationEyeTracked(out isEyeTracked);
#endif
#endif
                return isEyeTracked;
            }
            set
            {
#if UNITY_OPENXR_1_9_0
#if USE_XR_DISPLAY_FOVEATION_API
                List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
                SubsystemManager.GetSubsystems(xrDisplays);
                foreach (var display in xrDisplays)
                {
                    display.foveatedRenderingFlags = value 
                        ? XRDisplaySubsystem.FoveatedRenderingFlags.GazeAllowed 
                        : XRDisplaySubsystem.FoveatedRenderingFlags.None;
                }
#else
                NativeMethods.MetaSetFoveationEyeTracked(_xrSession, value);
#endif
#endif                
            }
        }
        
        public bool GetFoveationEyeTrackedCenter(ref Vector2 leftEye, ref Vector2 rightEye)
        {
            if (_xrGetFoveationEyeTrackedStateMETA == null)
            {
                _xrGetFoveationEyeTrackedStateMETA = ValveOpenXRSupportFeature.GetOpenXrInstanceProc<XrGetFoveationEyeTrackedStateMETADelegate>("xrGetFoveationEyeTrackedStateMETA");
            }

            if (_xrGetFoveationEyeTrackedStateMETA == null)
            {
                return false;
            }
            
            XrFoveationEyeTrackedStateMETA eyeTrackedState = new XrFoveationEyeTrackedStateMETA{type = XrStructureType.XR_TYPE_FOVEATION_EYE_TRACKED_STATE_META};
            XrResult result = _xrGetFoveationEyeTrackedStateMETA(_xrSession, ref eyeTrackedState);

            if (result != XrResult.Success || eyeTrackedState.flags != XrFoveationEyeTrackedStateFlagsMETA.XR_FOVEATION_EYE_TRACKED_STATE_VALID_BIT_META)
            {
                return false;
            }

            leftEye.Set(eyeTrackedState.foveationCenter[0].X, eyeTrackedState.foveationCenter[0].Y);
            rightEye.Set(eyeTrackedState.foveationCenter[1].X, eyeTrackedState.foveationCenter[1].Y);
            return true;
        }
        
#if UNITY_EDITOR
        protected override void GetValidationChecks(List<OpenXRFeature.ValidationRule> results, BuildTargetGroup target)
        {
            results.Add(new ValidationRule(this)
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

#if UNITY_6000_0_OR_NEWER
            results.Add(new ValidationRule(this)
            {
                message = "Unity Foveated Rendering feature must be enabled.",
                error = true,
                checkPredicate = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
                    if (settings == null)
                        return false;

                    var foveationFeature = settings.GetFeature<FoveatedRenderingFeature>();
                    return foveationFeature.enabled;
                },
                fixIt = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
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
        }        
#endif        

        protected override void OnSessionCreate(ulong xrSession)
        {
            _xrSession = xrSession;
        }
        
        protected override void OnSessionStateChange(int oldState, int newState)
        {
            if (oldState == (int)XrSessionState.XR_SESSION_STATE_VISIBLE &&
                newState == (int)XrSessionState.XR_SESSION_STATE_FOCUSED)
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
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct XrFoveationEyeTrackedStateMETA
        {
            public XrStructureType type;
            public IntPtr next;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=2)]
            public XrVector2f[] foveationCenter;
            public XrFoveationEyeTrackedStateFlagsMETA flags;
        }
        
        [Flags]
        private enum XrFoveationEyeTrackedStateFlagsMETA
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

    internal enum XrSessionState
    {
        XR_SESSION_STATE_UNKNOWN = 0,
        XR_SESSION_STATE_IDLE = 1,
        XR_SESSION_STATE_READY = 2,
        XR_SESSION_STATE_SYNCHRONIZED = 3,
        XR_SESSION_STATE_VISIBLE = 4,
        XR_SESSION_STATE_FOCUSED = 5,
        XR_SESSION_STATE_STOPPING = 6,
        XR_SESSION_STATE_LOSS_PENDING = 7,
        XR_SESSION_STATE_EXITING = 8,
        XR_SESSION_STATE_MAX_ENUM = 0x7FFFFFFF
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
