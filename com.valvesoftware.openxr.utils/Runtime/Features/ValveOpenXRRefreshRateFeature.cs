using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;
#if UNITY_EDITOR
using UnityEditor.XR.OpenXR.Features;
#endif


/// <summary>
/// Example feature showing how to intercept a single OpenXR function.
/// </summary>
#if UNITY_EDITOR
[OpenXRFeature(UiName = "Valve Utils: Refresh Rate",
    BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android },
    Company = "Valve Software",
    DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
    Desc = "Retrieve current hmd refresh rate",
    OpenxrExtensionStrings = "XR_FB_display_refresh_rate",
    Version = "1",
    FeatureId = featureId)]
#endif
public class ValveOpenXRRefreshRateFeature : OpenXRFeature
{
    public const string featureId = "com.valve.openxr.refreshrate";

    public bool initialized { get { return _initialized; } }

    private bool _initialized = false;

    private ulong instanceHandle = ulong.MaxValue;
    private ulong sessionHandle = ulong.MaxValue;

    internal delegate int Type_xrGetInstanceProcAddr(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string name, out IntPtr function);
    internal delegate int Type_xrGetDisplayRefreshRateFB(ulong session, out float displayRefreshRate);
    internal delegate int Type_xrRequestDisplayRefreshRateFB(ulong session, float displayRefreshRate);
    internal delegate int Type_xrEnumerateDisplayRefreshRatesFB(
        ulong session,
        uint displayRefreshRateCapacityInput,
        out uint displayRefreshRateCountOutput,
        [Out] float[] displayRefreshRates // float*
    );

    private Type_xrGetInstanceProcAddr xrGetInstanceProcAddrDelegate;
    private Type_xrGetDisplayRefreshRateFB xrGetDisplayRefreshRateFB;
    private Type_xrRequestDisplayRefreshRateFB xrRequestDisplayRefreshRateFB;
    private Type_xrEnumerateDisplayRefreshRatesFB xrEnumerateDisplayRefreshRatesFB;

    public event OnRefreshRateFeatureAvailableDelegate OnRefreshRateFeatureAvailable;

    protected override bool OnInstanceCreate(ulong xrInstance)
    {
        Debug.Log("Extension: XR_FB_display_refresh_rate - OnInstanceCreate");
        instanceHandle = xrInstance;

        return base.OnInstanceCreate(xrInstance);
    }

    protected override void OnSessionBegin(ulong xrSession)
    {
        Debug.Log("Extension: XR_FB_display_refresh_rate - OnSessionBegin: " + xrSession);
        if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_display_refresh_rate"))
        {
            Debug.LogError("ERROR: extension not enabled: XR_FB_display_refresh_rate");
            base.OnSessionBegin(xrSession);
            return;
        }

        sessionHandle = xrSession;

        InitializeFunctions();

        if (OnRefreshRateFeatureAvailable != null)
            OnRefreshRateFeatureAvailable();

        _initialized = true;

        base.OnSessionBegin(xrSession);
    }

    protected override void OnSessionDestroy(ulong xrSession)
    {
        _initialized = false;
        OnRefreshRateFeatureAvailable = null;
        base.OnSessionDestroy(xrSession);
    }

    protected override void OnSessionEnd(ulong xrSession)
    {
        _initialized = false;
        OnRefreshRateFeatureAvailable = null;
        base.OnSessionDestroy(xrSession);
    }

    private void InitializeFunctions()
    {
        xrGetInstanceProcAddrDelegate = (Type_xrGetInstanceProcAddr)Marshal.GetDelegateForFunctionPointer(xrGetInstanceProcAddr, typeof(Type_xrGetInstanceProcAddr));

        IntPtr getRefreshPointer = IntPtr.Zero;
        var result = xrGetInstanceProcAddrDelegate(instanceHandle, "xrGetDisplayRefreshRateFB", out getRefreshPointer);
        if (result != 0)
            Debug.LogError("ERROR: Error getting function pointer for: xrGetDisplayRefreshRateFB. " + result.ToString());

        xrGetDisplayRefreshRateFB = Marshal.GetDelegateForFunctionPointer<Type_xrGetDisplayRefreshRateFB>(getRefreshPointer);


        IntPtr requestRefreshPointer = IntPtr.Zero;
        result = xrGetInstanceProcAddrDelegate(instanceHandle, "xrRequestDisplayRefreshRateFB", out requestRefreshPointer);
        if (result != 0)
            Debug.LogError("ERROR: Error getting function pointer for: xrRequestDisplayRefreshRateFB. " + result.ToString());

        xrRequestDisplayRefreshRateFB = Marshal.GetDelegateForFunctionPointer<Type_xrRequestDisplayRefreshRateFB>(requestRefreshPointer);


        IntPtr enumerateDisplayRefreshRatesPointer = IntPtr.Zero;
        result = xrGetInstanceProcAddrDelegate(instanceHandle, "xrEnumerateDisplayRefreshRatesFB", out enumerateDisplayRefreshRatesPointer);
        if (result != 0)
            Debug.LogError("ERROR: Error getting function pointer for: xrEnumerateDisplayRefreshRatesFB. " + result.ToString());

        xrEnumerateDisplayRefreshRatesFB = Marshal.GetDelegateForFunctionPointer<Type_xrEnumerateDisplayRefreshRatesFB>(enumerateDisplayRefreshRatesPointer);
    }

    public float GetRefreshRate()
    {
        if (sessionHandle == ulong.MaxValue)
        {
            Debug.LogError("ERROR: RefreshRateFeature not set up.");
            if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_display_refresh_rate"))
                Debug.LogError("ERROR: extension not enabled: XR_FB_display_refresh_rate");

            return -1;
        }

        float refreshrate;
        var result = xrGetDisplayRefreshRateFB(sessionHandle, out refreshrate);

        if (result != 0)
            Debug.Log("WARNING: Result from xrGetDisplayRefreshRateFB was non-zero: " + result.ToString() + " :: " + refreshrate.ToString());

        return refreshrate;
    }

    public int SetRefreshRate(float refreshrate)
    {
        if (sessionHandle == ulong.MaxValue)
        {
            Debug.LogError("ERROR: RefreshRateFeature not set up.");
            if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_display_refresh_rate"))
                Debug.LogError("ERROR: extension not enabled: XR_FB_display_refresh_rate");

            return -1;
        }

        var result = xrRequestDisplayRefreshRateFB(sessionHandle, refreshrate);

        if (result != 0)
            Debug.Log("WARNING: Result from xrRequestDisplayRefreshRateFB was non-zero: " + result.ToString() + " :: " + refreshrate.ToString());

        return result;
    }

    public int EnumerateRefreshRates(ref List<float> displayRefreshRates)
    {
        if (sessionHandle == ulong.MaxValue)
        {
            Debug.LogError("ERROR: RefreshRateFeature not set up.");
            if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_display_refresh_rate"))
                Debug.LogError("ERROR: extension not enabled: XR_FB_display_refresh_rate");

            return -1;
        }

        displayRefreshRates.Clear();
        var result = xrEnumerateDisplayRefreshRatesFB(sessionHandle, 0, out var count, null);
        if (result != 0)
        {
            Debug.Log("WARNING: Result from xrEnumerateDisplayRefreshRatesFB was non-zero: " + result.ToString());
            return result;
        }

        var rates = new float[count];
        result = xrEnumerateDisplayRefreshRatesFB(sessionHandle, count, out count, rates);
        if (result != 0)
        {
            Debug.Log("WARNING: Result from xrEnumerateDisplayRefreshRatesFB(2) was non-zero: " + result.ToString());
            return result;
        }

        displayRefreshRates.AddRange(rates);
        return result;
    }
}

public delegate void OnRefreshRateFeatureAvailableDelegate();