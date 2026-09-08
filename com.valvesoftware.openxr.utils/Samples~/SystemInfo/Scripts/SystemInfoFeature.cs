using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;

namespace Valve.OpenXR.Utils
{
    #if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "Valve Utils: System Info Sample",
        Desc = "Support for querying the system info.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        Version = "0.1.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        OpenxrExtensionStrings = "",
        FeatureId = featureId)]
    #endif
    public class SystemInfoFeature : OpenXRFeature
    {
        public const string featureId = "com.valvesoftware.openxr.utils.sysinfo";

        private ulong _xrInstance;
        private ulong _xrSystemId;
        private PFN_xrGetSystemProperties _xrGetSystemProperties;
        
        private static SystemInfoFeature Instance => OpenXRSettings.ActiveBuildTargetInstance.GetFeature<SystemInfoFeature>();

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            _xrInstance = xrInstance;
            _xrGetSystemProperties = ValveOpenXRSupportFeature.GetOpenXrInstanceProc<PFN_xrGetSystemProperties>("xrGetSystemProperties");
            return base.OnInstanceCreate(xrInstance);
        }

        protected override void OnSystemChange(ulong xrSystem)
        {
            _xrSystemId = xrSystem;
            base.OnSystemChange(xrSystem);
        }

#if UNITY_EDITOR
        protected override void OnEnabledChange()
        {
            if (enabled)
            {
                var supportFeature = OpenXRSettings.ActiveBuildTargetInstance.GetFeature<ValveOpenXRSupportFeature>();
                supportFeature.enabled = true;
            }
        }

        protected override void GetValidationChecks(List<OpenXRFeature.ValidationRule> results, BuildTargetGroup target)
        {
            results.Add(new ValidationRule(this)
            {
                message = "Valve Utils rendering feature must be enabled.",
                error = true,
                checkPredicate = () =>
                {
                    var supportFeature = OpenXRSettings.ActiveBuildTargetInstance.GetFeature<ValveOpenXRSupportFeature>();
                    return supportFeature.enabled;
                },
                fixIt = () =>
                {
                    var supportFeature = OpenXRSettings.ActiveBuildTargetInstance.GetFeature<ValveOpenXRSupportFeature>();
                    supportFeature.enabled = true;
                },
                fixItAutomatic = true,
                fixItMessage = "Enable Valve Utils rendering feature"
            });
        }
#endif        
        
        public static bool IsInitialized()
        {
            return Instance.IsInitializedInternal();
        }
        
        public static bool IsRunnginOnSteamFrame()
        {
            string steamVRFamilyKey = "SteamVR/OpenXR";
            string steamVRDriverKey = "cv";

            return Instance.DoesHeadsetMatch(steamVRFamilyKey, steamVRDriverKey);
        }

        public static string GetHeadsetName()
        {
            return Instance.GetHeadsetNameFromOpenXR();
        }

        private string GetHeadsetNameFromOpenXR()
        {
            string systemName = null;

            if (_xrInstance != 0 && _xrSystemId != 0 && _xrGetSystemProperties != null)
            {
                XrSystemProperties properties = new XrSystemProperties
                {
                    type = XrStructureType.XR_TYPE_SYSTEM_PROPERTIES
                };

                _xrGetSystemProperties(_xrInstance, _xrSystemId, ref properties);

                systemName = properties.systemName;
            }

            return systemName;
        }

        private bool DoesHeadsetMatch(string familyKey, string driverKey)
        {
            string headsetName = GetHeadsetName();

            Debug.Log($"DoesHeadsetMatch - {headsetName}");
            
            if (!string.IsNullOrEmpty(headsetName))
            {
                string pattern = @"^(?<family>[^:]+)\s*:\s*(?<driver>.+)$";
                Match match = Regex.Match(headsetName, pattern);

                if (match.Success)
                {
                    string family = match.Groups["family"].Value.Trim();
                    string driver = match.Groups["driver"].Value.Trim();
            
                    return family.Equals(familyKey, StringComparison.OrdinalIgnoreCase) &&
                           driver.Equals(driverKey, StringComparison.OrdinalIgnoreCase);            
                }
            }

            return false;
        }

        private bool IsInitializedInternal()
        {
            return enabled && _xrInstance != 0 && _xrSystemId != 0 && _xrGetSystemProperties != null;
        }
        
    #region OpenXR Structures

        public enum XrStructureType : uint
        {
            XR_TYPE_SYSTEM_PROPERTIES = 5,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XrSystemProperties
      
        {
            private const int XR_MAX_SYSTEM_NAME_SIZE = 256;
            public XrStructureType type;
            public IntPtr next;
            public ulong systemId; // XrSystemId
            public uint vendorId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = XR_MAX_SYSTEM_NAME_SIZE)]
            public string systemName;
            public XrSystemGraphicsProperties graphicsProperties;
            public XrSystemTrackingProperties trackingProperties;
        }    

        [StructLayout(LayoutKind.Sequential)]
        public struct XrSystemGraphicsProperties
        {
            public uint maxSwapchainImageHeight;
            public uint maxSwapchainImageWidth;
            public uint maxLayerCount;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct XrSystemTrackingProperties
        {
            public uint orientationTracking;
            public uint positionTracking;
        };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int PFN_xrGetSystemProperties(ulong instance, ulong systemId, ref XrSystemProperties properties);

    #endregion
    }
}