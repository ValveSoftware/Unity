using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.NativeTypes;
#if UNITY_EDITOR
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Reads and sets the display refresh rate through XR_FB_display_refresh_rate.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: Refresh Rate",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android },
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        Desc = "Retrieve current hmd refresh rate",
        OpenxrExtensionStrings = "XR_FB_display_refresh_rate",
        Version = "1.1.0",
        FeatureId = featureId)]
#endif
    public class ValveOpenXRRefreshRateFeature : OpenXRFeature
    {
        public const string featureId = "com.valve.openxr.refreshrate";

        public bool initialized { get { return _initialized; } }

        private bool _initialized = false;

        private ulong instanceHandle = ulong.MaxValue;
        private ulong sessionHandle = ulong.MaxValue;

        internal delegate XrResult Type_xrGetDisplayRefreshRateFB(ulong session, out float displayRefreshRate);
        internal delegate XrResult Type_xrRequestDisplayRefreshRateFB(ulong session, float displayRefreshRate);
        internal delegate XrResult Type_xrEnumerateDisplayRefreshRatesFB(
            ulong session,
            uint displayRefreshRateCapacityInput,
            out uint displayRefreshRateCountOutput,
            [Out] float[] displayRefreshRates // float*
        );

        private Type_xrGetDisplayRefreshRateFB xrGetDisplayRefreshRateFB;
        private Type_xrRequestDisplayRefreshRateFB xrRequestDisplayRefreshRateFB;
        private Type_xrEnumerateDisplayRefreshRatesFB xrEnumerateDisplayRefreshRatesFB;

        // Fixed per session, enumerated on first request.
        private float[] supportedRefreshRates;

        public event OnRefreshRateFeatureAvailableDelegate OnRefreshRateFeatureAvailable;

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            instanceHandle = xrInstance;

            return base.OnInstanceCreate(xrInstance);
        }

        protected override void OnSessionBegin(ulong xrSession)
        {
            if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_display_refresh_rate"))
            {
                base.OnSessionBegin(xrSession);
                return;
            }

            sessionHandle = xrSession;

            if (!InitializeFunctions())
            {
                sessionHandle = ulong.MaxValue;
                base.OnSessionBegin(xrSession);
                return;
            }

            _initialized = true;

            if (OnRefreshRateFeatureAvailable != null)
            {
                try
                {
                    OnRefreshRateFeatureAvailable();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            base.OnSessionBegin(xrSession);
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            _initialized = false;
            OnRefreshRateFeatureAvailable = null;
            sessionHandle = ulong.MaxValue;
            supportedRefreshRates = null;
            ClearFunctions();
            base.OnSessionDestroy(xrSession);
        }

        protected override void OnSessionEnd(ulong xrSession)
        {
            _initialized = false;
            base.OnSessionEnd(xrSession);
        }

        private bool InitializeFunctions()
        {
            if (!ValveOpenXRDelegates.TryGetInstanceProc(xrGetInstanceProcAddr, instanceHandle, "xrGetDisplayRefreshRateFB", out xrGetDisplayRefreshRateFB) ||
                !ValveOpenXRDelegates.TryGetInstanceProc(xrGetInstanceProcAddr, instanceHandle, "xrRequestDisplayRefreshRateFB", out xrRequestDisplayRefreshRateFB) ||
                !ValveOpenXRDelegates.TryGetInstanceProc(xrGetInstanceProcAddr, instanceHandle, "xrEnumerateDisplayRefreshRatesFB", out xrEnumerateDisplayRefreshRatesFB))
            {
                ClearFunctions();
                return false;
            }

            return true;
        }

        private void ClearFunctions()
        {
            xrGetDisplayRefreshRateFB = null;
            xrRequestDisplayRefreshRateFB = null;
            xrEnumerateDisplayRefreshRatesFB = null;
        }

        public float GetRefreshRate()
        {
            if (sessionHandle == ulong.MaxValue)
            {
                if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_display_refresh_rate"))
                {
                    Debug.LogError("ERROR: extension not enabled: XR_FB_display_refresh_rate");
                }
                else
                {
                    Debug.LogError("ERROR: RefreshRateFeature not set up.");
                }

                return -1;
            }

            float refreshrate;
            XrResult result = xrGetDisplayRefreshRateFB(sessionHandle, out refreshrate);

            if (result != XrResult.Success)
            {
                Debug.Log("WARNING: Result from xrGetDisplayRefreshRateFB was non-zero: " + result.ToString() + " :: " + refreshrate.ToString());
            }

            return refreshrate;
        }

        public int SetRefreshRate(float refreshrate)
        {
            if (sessionHandle == ulong.MaxValue)
            {
                if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_display_refresh_rate"))
                {
                    Debug.LogError("ERROR: extension not enabled: XR_FB_display_refresh_rate");
                }
                else
                {
                    Debug.LogError("ERROR: RefreshRateFeature not set up.");
                }

                return -1;
            }

            XrResult result = xrRequestDisplayRefreshRateFB(sessionHandle, refreshrate);

            if (result != XrResult.Success)
            {
                Debug.Log("WARNING: Result from xrRequestDisplayRefreshRateFB was non-zero: " + result.ToString() + " :: " + refreshrate.ToString());
            }

            return (int)result;
        }

        public int EnumerateRefreshRates(ref List<float> displayRefreshRates)
        {
            if (sessionHandle == ulong.MaxValue)
            {
                if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_display_refresh_rate"))
                {
                    Debug.LogError("ERROR: extension not enabled: XR_FB_display_refresh_rate");
                }
                else
                {
                    Debug.LogError("ERROR: RefreshRateFeature not set up.");
                }

                return -1;
            }

            displayRefreshRates.Clear();
            if (supportedRefreshRates == null || supportedRefreshRates.Length == 0)
            {
                XrResult result = xrEnumerateDisplayRefreshRatesFB(sessionHandle, 0, out uint count, null);
                if (result != XrResult.Success)
                {
                    Debug.Log("WARNING: Result from xrEnumerateDisplayRefreshRatesFB was non-zero: " + result.ToString());
                    return (int)result;
                }

                float[] rates = new float[count];
                result = xrEnumerateDisplayRefreshRatesFB(sessionHandle, count, out count, rates);
                if (result != XrResult.Success)
                {
                    Debug.Log("WARNING: Result from xrEnumerateDisplayRefreshRatesFB(2) was non-zero: " + result.ToString());
                    return (int)result;
                }

                Array.Resize(ref rates, (int)count);
                supportedRefreshRates = rates;
            }

            displayRefreshRates.AddRange(supportedRefreshRates);
            return (int)XrResult.Success;
        }
    }

    public delegate void OnRefreshRateFeatureAvailableDelegate();
}