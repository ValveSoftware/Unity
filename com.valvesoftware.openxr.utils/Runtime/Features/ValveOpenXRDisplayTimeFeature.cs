using System;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Hooks xrWaitFrame to expose the frame's predicted display time, which Unity's OpenXR plugin does not pass on. Use it for xrLocateSpace so
    /// poses match Unity's own.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: Display Time",
        Desc = "Exposes the frame's predicted display time from xrWaitFrame to other OpenXR features.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        Version = "0.1.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        FeatureId = featureId
    )]
#endif
    public class ValveOpenXRDisplayTimeFeature : OpenXRFeature
    {
        public const string featureId = "com.valvesoftware.openxr.utils.display_time";

        private const string LogPrefix = "[" + nameof(ValveOpenXRDisplayTimeFeature) + "] ";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult WaitFrameDelegate(ulong session, IntPtr frameWaitInfo, IntPtr frameState);

        [StructLayout(LayoutKind.Sequential)]
        private struct XrFrameState
        {
            public XrStructureType type;
            public IntPtr next;
            public long predictedDisplayTime;
            public long predictedDisplayPeriod;
            public uint shouldRender;
        }

        private static readonly int PredictedDisplayTimeOffset = (int)Marshal.OffsetOf(typeof(XrFrameState), nameof(XrFrameState.predictedDisplayTime));
        private static readonly int PredictedDisplayPeriodOffset = (int)Marshal.OffsetOf(typeof(XrFrameState), nameof(XrFrameState.predictedDisplayPeriod));

        // Static, called from native code on any thread. Kept referenced so the thunks stay valid.
        private static ValveOpenXRDelegates.GetInstanceProcAddrDelegate s_nextGetInstanceProcAddr;
        private static ValveOpenXRDelegates.GetInstanceProcAddrDelegate s_getInstanceProcAddrHook;
        private static IntPtr s_getInstanceProcAddrHookPtr;
        private static WaitFrameDelegate s_nextWaitFrame;
        private static WaitFrameDelegate s_waitFrameHook;
        private static IntPtr s_waitFrameHookPtr;

        private static bool s_hookInstalled;
        private static bool s_waitFrameHooked;
        private static bool s_warnedAboutFrameState;
        private static long s_predictedDisplayTime;
        private static long s_predictedDisplayPeriod;

        /// <summary>True while the feature is enabled and hooked.</summary>
        public static bool IsHookInstalled
        {
            get { return s_hookInstalled; }
        }

        /// <summary>True once a frame has been waited on through the hook this session.</summary>
        public static bool IsAvailable
        {
            get { return s_waitFrameHooked && Volatile.Read(ref s_predictedDisplayTime) != 0; }
        }

        /// <summary>
        /// Predicted display time of the latest xrWaitFrame, as XrTime. False before the first frame or when disabled. Read once per frame and reuse.
        /// </summary>
        public static bool TryGetPredictedDisplayTime(out long predictedDisplayTime)
        {
            predictedDisplayTime = Volatile.Read(ref s_predictedDisplayTime);
            return s_waitFrameHooked && predictedDisplayTime != 0;
        }

        /// <summary>Predicted display period in nanoseconds from the latest xrWaitFrame. False as for <see cref="TryGetPredictedDisplayTime"/>.</summary>
        public static bool TryGetPredictedDisplayPeriod(out long predictedDisplayPeriod)
        {
            predictedDisplayPeriod = Volatile.Read(ref s_predictedDisplayPeriod);
            return s_waitFrameHooked && predictedDisplayPeriod != 0;
        }

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            s_hookInstalled = false;
            s_waitFrameHooked = false;
            if (!ValveOpenXRDelegates.TryFromFunctionPointer(func, out s_nextGetInstanceProcAddr))
            {
                Debug.LogError(LogPrefix + "Cannot wrap the next xrGetInstanceProcAddr in the chain. The predicted display time will not be available.");
                return func;
            }

            if (s_getInstanceProcAddrHook == null)
            {
                s_getInstanceProcAddrHook = GetInstanceProcAddrHook;
                s_getInstanceProcAddrHookPtr = Marshal.GetFunctionPointerForDelegate(s_getInstanceProcAddrHook);
                s_waitFrameHook = WaitFrameHook;
                s_waitFrameHookPtr = Marshal.GetFunctionPointerForDelegate(s_waitFrameHook);
            }

            s_hookInstalled = true;
            return s_getInstanceProcAddrHookPtr;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            s_hookInstalled = false;
            s_waitFrameHooked = false;
            ClearFrameState();
        }

        protected override void OnSessionBegin(ulong xrSession)
        {
            ClearFrameState();
        }

        protected override void OnSessionEnd(ulong xrSession)
        {
            ClearFrameState();
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            ClearFrameState();
        }

        private static void ClearFrameState()
        {
            Volatile.Write(ref s_predictedDisplayTime, 0);
            Volatile.Write(ref s_predictedDisplayPeriod, 0);
        }

        [MonoPInvokeCallback(typeof(ValveOpenXRDelegates.GetInstanceProcAddrDelegate))]
        private static int GetInstanceProcAddrHook(ulong instance, string name, out IntPtr procAddr)
        {
            int result = s_nextGetInstanceProcAddr(instance, name, out procAddr);
            if (result < 0 || procAddr == IntPtr.Zero || name != "xrWaitFrame" || procAddr == s_waitFrameHookPtr)
            {
                return result;
            }

            if (!ValveOpenXRDelegates.TryFromFunctionPointer(procAddr, out s_nextWaitFrame))
            {
                Debug.LogError(LogPrefix + "Cannot wrap xrWaitFrame. The predicted display time will not be available.");
                return result;
            }

            procAddr = s_waitFrameHookPtr;
            s_waitFrameHooked = true;
            return result;
        }

        [MonoPInvokeCallback(typeof(WaitFrameDelegate))]
        private static XrResult WaitFrameHook(ulong session, IntPtr frameWaitInfo, IntPtr frameState)
        {
            XrResult result = s_nextWaitFrame(session, frameWaitInfo, frameState);
            if (!result.IsSuccess() || frameState == IntPtr.Zero)
            {
                return result;
            }

            // Must not throw into the plugin's native frame loop.
            try
            {
                Volatile.Write(ref s_predictedDisplayPeriod, Marshal.ReadInt64(frameState, PredictedDisplayPeriodOffset));
                Volatile.Write(ref s_predictedDisplayTime, Marshal.ReadInt64(frameState, PredictedDisplayTimeOffset));
            }
            catch (Exception exception)
            {
                if (!s_warnedAboutFrameState)
                {
                    s_warnedAboutFrameState = true;
                    Debug.LogError(LogPrefix + "Failed to read XrFrameState: " + exception.Message);
                }
            }

            return result;
        }
    }
}
