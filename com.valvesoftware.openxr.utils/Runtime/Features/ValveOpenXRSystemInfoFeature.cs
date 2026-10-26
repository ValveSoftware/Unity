using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
    /// Reports the OpenXR system name and whether the app runs locally on Steam Frame.
    /// Warning: You should only be using this for graphics quality checks. If you are checking controller type check the controller interaction profile instead.
    ///          If you are using it for something else let us know.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: System Info",
        Desc = "Query the OpenXR system name and whether the app is running locally on Steam Frame.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        Version = "1.0.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        OpenxrExtensionStrings = "",
        FeatureId = featureId)]
#endif
    public class ValveOpenXRSystemInfoFeature : OpenXRFeature
    {
        public const string featureId = "com.valvesoftware.openxr.utils.sysinfo";

        private const string SteamVRFamilyKey = "SteamVR/OpenXR";
        private const string SteamFrameDriverKey = "cv";

        private ulong _xrInstance;
        private ulong _xrSystemId;
        private PFN_xrGetSystemProperties _xrGetSystemProperties;
        // Fixed per system. Each query marshals a 256 character string.
        private string _systemName;
        private bool _localSteamFrameChecked;
        private bool _isLocalSteamFrame;

        // OpenXRSettings.Instance is the running configuration, ActiveBuildTargetInstance differs in the Editor.
        private static ValveOpenXRSystemInfoFeature Instance
        {
            get
            {
                return OpenXRSettings.Instance.GetFeature<ValveOpenXRSystemInfoFeature>();
            }
        }

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            _xrInstance = xrInstance;
            ValveOpenXRDelegates.TryGetInstanceProc(xrGetInstanceProcAddr, xrInstance, "xrGetSystemProperties", out _xrGetSystemProperties);
            return base.OnInstanceCreate(xrInstance);
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            _xrInstance = 0;
            _xrSystemId = 0;
            _xrGetSystemProperties = null;
            ResetSystemCache();
            base.OnInstanceDestroy(xrInstance);
        }

        protected override void OnSystemChange(ulong xrSystem)
        {
            _xrSystemId = xrSystem;
            ResetSystemCache();
            base.OnSystemChange(xrSystem);
        }

        /// <summary>
        /// True once the feature is enabled and has an OpenXR instance and system to query. The other queries return null or false before that.
        /// </summary>
        public static bool IsInitialized()
        {
            if (Instance == null)
            {
                return false;
            }

            return Instance.IsInitializedInternal();
        }

        /// <summary>
        /// True when the OpenXR system name is what runs for Steam Frame and the app is running locally on Steam Frame, false while streaming from a PC.
        /// Warning: You should only be using this for graphics quality checks. If you are checking controller type check the controller interaction profile instead.
        /// </summary>
        public static bool IsLocalSteamFrame()
        {
            if (Instance == null)
            {
                return false;
            }

            return Instance.IsLocalSteamFrameInternal();
        }

        /// <summary>The OpenXR system name, or null before the instance and system exist.</summary>
        public static string GetSystemName()
        {
            if (Instance == null)
            {
                return null;
            }

            return Instance.GetSystemNameFromOpenXR();
        }

        private bool IsLocalSteamFrameInternal()
        {
            if (!_localSteamFrameChecked && GetSystemNameFromOpenXR() != null)
            {
                _isLocalSteamFrame = IsSteamVRDriverCV() && IsSteamFrameHardware();
                _localSteamFrameChecked = true;
            }

            return _isLocalSteamFrame;
        }

        // True when the OpenXR system name is what runs for Steam Frame, whether the app runs on the Frame or streams to it from a PC.
        private bool IsSteamVRDriverCV()
        {
            return DoesSystemMatch(SteamVRFamilyKey, SteamFrameDriverKey);
        }

        // Unity reports the graphics device as "Turnip Adreno (TM) 750" on the Frame.
        private static bool IsSteamFrameHardware()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return false;
            }

            string deviceName = SystemInfo.graphicsDeviceName;

            if (string.IsNullOrEmpty(deviceName))
            {
                return false;
            }

            return Regex.IsMatch(deviceName, @"\bAdreno\b.*\b750\b", RegexOptions.IgnoreCase);
        }

        private void ResetSystemCache()
        {
            _systemName = null;
            _localSteamFrameChecked = false;
            _isLocalSteamFrame = false;
        }

        private string GetSystemNameFromOpenXR()
        {
            if (_systemName == null && _xrInstance != 0 && _xrSystemId != 0 && _xrGetSystemProperties != null)
            {
                XrSystemProperties properties = new XrSystemProperties
                {
                    type = XrStructureType.SystemProperties
                };

                if (_xrGetSystemProperties(_xrInstance, _xrSystemId, ref properties) == XrResult.Success)
                {
                    _systemName = properties.systemName;
                }
            }

            return _systemName;
        }

        private bool DoesSystemMatch(string familyKey, string driverKey)
        {
            string systemName = GetSystemNameFromOpenXR();

            if (!string.IsNullOrEmpty(systemName))
            {
                string pattern = @"^(?<family>[^:]+)\s*:\s*(?<driver>.+)$";
                Match match = Regex.Match(systemName, pattern);

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

        [StructLayout(LayoutKind.Sequential)]
        private struct XrSystemProperties
        {
            private const int XR_MAX_SYSTEM_NAME_SIZE = 256;
            public XrStructureType type;
            public IntPtr next;
            public ulong systemId;
            public uint vendorId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = XR_MAX_SYSTEM_NAME_SIZE)]
            public string systemName;
            public XrSystemGraphicsProperties graphicsProperties;
            public XrSystemTrackingProperties trackingProperties;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrSystemGraphicsProperties
        {
            public uint maxSwapchainImageHeight;
            public uint maxSwapchainImageWidth;
            public uint maxLayerCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrSystemTrackingProperties
        {
            public uint orientationTracking;
            public uint positionTracking;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrGetSystemProperties(ulong instance, ulong systemId, ref XrSystemProperties properties);

#endregion
    }
}
