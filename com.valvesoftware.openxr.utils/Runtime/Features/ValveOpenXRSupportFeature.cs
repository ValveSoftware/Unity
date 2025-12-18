using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.NativeTypes;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Enables the Valve OpenXR Loader support.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: Settings for Unity's Rendering",
        Desc = "Settings for OpenXR Rendering.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        OpenxrExtensionStrings = "",
        Version = "0.1.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        FeatureId = featureId
    )]
#endif
    public class ValveOpenXRSupportFeature : OpenXRFeature
    {
        public const string featureId = "com.valvesoftware.openxr.utils.support";

        [SerializeField]
        [Tooltip("Optimization that allows 4x MSAA textures to be memoryless on Vulkan")]
        internal bool optimizeBufferDiscards;
        
        [SerializeField]
        [Tooltip("Vulkan only")]
        internal bool lateLatchingMode;

        [SerializeField]
        [Tooltip("Vulkan only")]
        internal bool lateLatchingDebug;

        private ulong _xrInstance;
        private ulong _xrSession;

        private ulong XrInstance => _xrInstance;
        private ulong XrSession => _xrSession;
        
        private delegate XrResult GetInstanceProcAddrDelegate(ulong instance, string name, ref IntPtr procAddr);
        private GetInstanceProcAddrDelegate _getInstanceProcAddr;

        private System.Type[] incompatibleFeatureTypes = { typeof(MetaQuestFeature) };
        
        internal static ValveOpenXRSupportFeature Instance
        {
            get
            {
                OpenXRSettings settings = OpenXRSettings.ActiveBuildTargetInstance;
                var features = new List<ValveOpenXRSupportFeature>();
                return settings.GetFeatures(features) > 0 ? features.First() : null;
            }
        }

        #region "Public OpenXR Support"

        public static bool HasOpenXRInstance() { return Instance.XrInstance != 0; }
        public static ulong GetOpenXRInstance() { return Instance.XrInstance; }
        public static event InstanceCreated OnInstanceCreated;
        public static event InstanceDestroyed OnInstanceDestroyed;

        public static bool HasSession() { return Instance.XrSession != 0; }
        public static ulong GetSession() { return Instance.XrSession; }
        public static event SessionCreated OnSessionCreated;
        public static event SessionDestroyed OnSessionDestroyed;

        public static T GetOpenXrInstanceProc<T>(string procName)
        {
            var instanceProc = GetOpenXrInstanceProc(procName);

            return instanceProc != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(instanceProc) : default;
        }

        public static IntPtr GetOpenXrInstanceProc(string procName)
        {
            return Instance.GetOpenXrInstanceProcInternal(procName);
        }
        
        #endregion

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            _xrInstance = xrInstance;
            OnInstanceCreated?.Invoke(xrInstance);
            return true;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            OnInstanceDestroyed?.Invoke(xrInstance);
            _xrInstance = 0;
        }
        
        protected override void OnSessionCreate(ulong xrSession)
        {
            _xrSession = xrSession;
            OnSessionCreated?.Invoke(_xrSession);
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            OnSessionDestroyed?.Invoke(_xrSession);
            _xrSession = 0;
        }
        
        private IntPtr GetOpenXrInstanceProcInternal(string procName)
        {
            if (_getInstanceProcAddr == null)
            {
                if (xrGetInstanceProcAddr == IntPtr.Zero)
                {
                    Debug.LogWarning($"Unity's OpenXR GetInstance function accessor is invalid!");
                    return IntPtr.Zero;
                }

                _getInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<GetInstanceProcAddrDelegate>(xrGetInstanceProcAddr);
            }

            IntPtr procAddr = IntPtr.Zero;
            
            if (_getInstanceProcAddr != null)
            {
                XrResult result = _getInstanceProcAddr(_xrInstance, procName, ref procAddr);

                if (result < 0)
                {
                    Debug.LogWarning($"Failed to find OpenXR instance function '{procName}'");
                    return IntPtr.Zero;
                }
            }
            
            return procAddr;
        }
        
        
#if UNITY_EDITOR
        protected override void GetValidationChecks(List<ValidationRule> rules, BuildTargetGroup targetGroup)
        {
            rules.Add(new ValidationRule(this)
            {
                message = "Late latching is only supported on Vulkan graphics API.",
                error = true,
                checkPredicate = () => (!lateLatchingMode && !lateLatchingDebug) || EditorUtils.UsesVulkan(BuildTarget.Android),
                fixIt = () =>
                {
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
                },
                fixItAutomatic = true,
                fixItMessage = "Set Vulkan as Graphics API"
            });
        }
#endif
    }
    
    internal enum XrStructureType
    {
        XR_TYPE_FOVEATION_EYE_TRACKED_STATE_META = 1000200001,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XrVector2f
    {
        public float X;
        public float Y;
    };
    
    public delegate void InstanceCreated(ulong instance);
    public delegate void InstanceDestroyed(ulong instance);
    public delegate void SessionCreated(ulong session);
    public delegate void SessionDestroyed(ulong session);
}
