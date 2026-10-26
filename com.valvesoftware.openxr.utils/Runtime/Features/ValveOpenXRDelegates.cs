using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Turns OpenXR function pointers into delegates and resolves instance functions.
    /// </summary>
    /// <remarks>
    /// When another feature hooks <c>xrGetInstanceProcAddr</c> with a managed delegate, <c>Marshal.GetDelegateForFunctionPointer</c> returns that
    /// delegate and a cast to the requested type throws. This class binds to it instead.
    /// </remarks>
    public static class ValveOpenXRDelegates
    {
        /// <summary>Same shape as the hooks installed through <c>HookGetInstanceProcAddr</c>.</summary>
        public delegate int GetInstanceProcAddrDelegate(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string name, out IntPtr procAddr);

        /// <summary>
        /// Resolves <paramref name="name"/> through <paramref name="xrGetInstanceProcAddr"/> as a <typeparamref name="T"/>. Logs and returns false
        /// when unavailable.
        /// </summary>
        public static bool TryGetInstanceProc<T>(IntPtr xrGetInstanceProcAddr, ulong instance, string name, out T function) where T : Delegate
        {
            function = null;
            if (xrGetInstanceProcAddr == IntPtr.Zero)
            {
                Debug.LogError("Unity's OpenXR xrGetInstanceProcAddr accessor is null.");
                return false;
            }

            if (!TryFromFunctionPointer(xrGetInstanceProcAddr, out GetInstanceProcAddrDelegate getInstanceProcAddr))
            {
                return false;
            }

            int result = getInstanceProcAddr(instance, name, out IntPtr procAddr);
            if (result < 0 || procAddr == IntPtr.Zero)
            {
                Debug.LogWarning($"The OpenXR function '{name}' is not available (result {result}).");
                return false;
            }

            return TryFromFunctionPointer(procAddr, out function);
        }

        /// <summary>
        /// Wraps <paramref name="functionPointer"/> as a <typeparamref name="T"/>, native or managed. Logs and returns false when it cannot.
        /// </summary>
        public static bool TryFromFunctionPointer<T>(IntPtr functionPointer, out T result) where T : Delegate
        {
            result = null;
            if (functionPointer == IntPtr.Zero)
            {
                Debug.LogError("The OpenXR function pointer is null.");
                return false;
            }

            Delegate raw = Marshal.GetDelegateForFunctionPointer(functionPointer, typeof(T));
            if (raw is T typed)
            {
                result = typed;
                return true;
            }

            // Another delegate type with the same signature: bind ours to its Invoke.
            MethodInfo invoke = raw.GetType().GetMethod("Invoke");
            result = Delegate.CreateDelegate(typeof(T), raw, invoke, throwOnBindFailure: false) as T;
            if (result == null)
            {
                Debug.LogError($"The OpenXR function pointer is a managed delegate of type {raw.GetType()} whose signature does not match {typeof(T)}.");
                return false;
            }

            return true;
        }
    }
}
