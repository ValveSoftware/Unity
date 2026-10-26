using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace Valve.OpenXR.Utils
{
    /// <summary>Native interop for <see cref="ValveOpenXRRenderModelFeature"/>. Layouts follow the OpenXR specification.</summary>
    public partial class ValveOpenXRRenderModelFeature : OpenXRFeature
    {
        #region Function pointer resolution

        // OpenXR core
        private PFN_xrLocateSpace _xrLocateSpace;
        private PFN_xrDestroySpace _xrDestroySpace;
        // XR_EXT_render_model
        private PFN_xrCreateRenderModelEXT _xrCreateRenderModelEXT;
        private PFN_xrDestroyRenderModelEXT _xrDestroyRenderModelEXT;
        private PFN_xrGetRenderModelPropertiesEXT _xrGetRenderModelPropertiesEXT;
        private PFN_xrCreateRenderModelSpaceEXT _xrCreateRenderModelSpaceEXT;
        private PFN_xrGetRenderModelStateEXT _xrGetRenderModelStateEXT;
        private PFN_xrCreateRenderModelAssetEXT _xrCreateRenderModelAssetEXT;
        private PFN_xrDestroyRenderModelAssetEXT _xrDestroyRenderModelAssetEXT;
        private PFN_xrGetRenderModelAssetDataEXT _xrGetRenderModelAssetDataEXT;
        private PFN_xrGetRenderModelAssetPropertiesEXT _xrGetRenderModelAssetPropertiesEXT;
        // XR_EXT_interaction_render_model
        private PFN_xrEnumerateInteractionRenderModelIdsEXT _xrEnumerateInteractionRenderModelIdsEXT;
        private PFN_xrEnumerateRenderModelSubactionPathsEXT _xrEnumerateRenderModelSubactionPathsEXT;
        private PFN_xrGetRenderModelPoseTopLevelUserPathEXT _xrGetRenderModelPoseTopLevelUserPathEXT;

        private bool ResolveInstanceProcs()
        {
            _xrLocateSpace = GetOpenXrInstanceProc<PFN_xrLocateSpace>("xrLocateSpace");
            _xrDestroySpace = GetOpenXrInstanceProc<PFN_xrDestroySpace>("xrDestroySpace");
            _xrCreateRenderModelEXT = GetOpenXrInstanceProc<PFN_xrCreateRenderModelEXT>("xrCreateRenderModelEXT");
            _xrDestroyRenderModelEXT = GetOpenXrInstanceProc<PFN_xrDestroyRenderModelEXT>("xrDestroyRenderModelEXT");
            _xrGetRenderModelPropertiesEXT = GetOpenXrInstanceProc<PFN_xrGetRenderModelPropertiesEXT>("xrGetRenderModelPropertiesEXT");
            _xrCreateRenderModelSpaceEXT = GetOpenXrInstanceProc<PFN_xrCreateRenderModelSpaceEXT>("xrCreateRenderModelSpaceEXT");
            _xrGetRenderModelStateEXT = GetOpenXrInstanceProc<PFN_xrGetRenderModelStateEXT>("xrGetRenderModelStateEXT");
            _xrCreateRenderModelAssetEXT = GetOpenXrInstanceProc<PFN_xrCreateRenderModelAssetEXT>("xrCreateRenderModelAssetEXT");
            _xrDestroyRenderModelAssetEXT = GetOpenXrInstanceProc<PFN_xrDestroyRenderModelAssetEXT>("xrDestroyRenderModelAssetEXT");
            _xrGetRenderModelAssetDataEXT = GetOpenXrInstanceProc<PFN_xrGetRenderModelAssetDataEXT>("xrGetRenderModelAssetDataEXT");
            _xrGetRenderModelAssetPropertiesEXT = GetOpenXrInstanceProc<PFN_xrGetRenderModelAssetPropertiesEXT>("xrGetRenderModelAssetPropertiesEXT");
            _xrEnumerateInteractionRenderModelIdsEXT =
                GetOpenXrInstanceProc<PFN_xrEnumerateInteractionRenderModelIdsEXT>("xrEnumerateInteractionRenderModelIdsEXT");
            _xrEnumerateRenderModelSubactionPathsEXT =
                GetOpenXrInstanceProc<PFN_xrEnumerateRenderModelSubactionPathsEXT>("xrEnumerateRenderModelSubactionPathsEXT");
            _xrGetRenderModelPoseTopLevelUserPathEXT =
                GetOpenXrInstanceProc<PFN_xrGetRenderModelPoseTopLevelUserPathEXT>("xrGetRenderModelPoseTopLevelUserPathEXT");

            bool resolved = _xrLocateSpace != null &&
                            _xrDestroySpace != null &&
                            _xrCreateRenderModelEXT != null &&
                            _xrDestroyRenderModelEXT != null &&
                            _xrGetRenderModelPropertiesEXT != null &&
                            _xrCreateRenderModelSpaceEXT != null &&
                            _xrGetRenderModelStateEXT != null &&
                            _xrCreateRenderModelAssetEXT != null &&
                            _xrDestroyRenderModelAssetEXT != null &&
                            _xrGetRenderModelAssetDataEXT != null &&
                            _xrGetRenderModelAssetPropertiesEXT != null &&
                            _xrEnumerateInteractionRenderModelIdsEXT != null &&
                            _xrEnumerateRenderModelSubactionPathsEXT != null &&
                            _xrGetRenderModelPoseTopLevelUserPathEXT != null;

            if (!resolved)
            {
                Debug.LogError(LogPrefix + "Failed to resolve the render model extension functions. Render models will not be available.");
                ClearInstanceProcs();
            }

            return resolved;
        }

        private void ClearInstanceProcs()
        {
            _xrLocateSpace = null;
            _xrDestroySpace = null;
            _xrCreateRenderModelEXT = null;
            _xrDestroyRenderModelEXT = null;
            _xrGetRenderModelPropertiesEXT = null;
            _xrCreateRenderModelSpaceEXT = null;
            _xrGetRenderModelStateEXT = null;
            _xrCreateRenderModelAssetEXT = null;
            _xrDestroyRenderModelAssetEXT = null;
            _xrGetRenderModelAssetDataEXT = null;
            _xrGetRenderModelAssetPropertiesEXT = null;
            _xrEnumerateInteractionRenderModelIdsEXT = null;
            _xrEnumerateRenderModelSubactionPathsEXT = null;
            _xrGetRenderModelPoseTopLevelUserPathEXT = null;
        }

        private T GetOpenXrInstanceProc<T>(string procName) where T : Delegate
        {
            ValveOpenXRDelegates.TryGetInstanceProc(xrGetInstanceProcAddr, _xrInstance, procName, out T function);
            return function;
        }

        #endregion

        #region OpenXR structures

        private const ulong XR_NULL_PATH = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct XrSpaceLocation
        {
            public XrStructureType type;
            public IntPtr next;
            public ulong locationFlags;      // XrSpaceLocationFlags, 64 bits wide in the C API
            public XrPosef pose;
        }

        // Guid(byte[]) is little-endian, so the text differs from the UUID string. Still unique and stable.
        private static Guid ToGuid(XrUuid uuid)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(uuid.dataPart1).CopyTo(bytes, 0);
            BitConverter.GetBytes(uuid.dataPart2).CopyTo(bytes, 8);
            return new Guid(bytes);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelCreateInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
            public ulong renderModelId;      // XrRenderModelIdEXT
            public uint gltfExtensionCount;
            public IntPtr gltfExtensions;    // const char* const*
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelPropertiesGetInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelPropertiesEXT
        {
            public XrStructureType type;
            public IntPtr next;
            public XrUuid cacheId;
            public uint animatableNodeCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelSpaceCreateInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
            public ulong renderModel;        // XrRenderModelEXT
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelStateGetInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
            public long displayTime;         // XrTime
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelNodeStateEXT
        {
            public XrPosef nodePose;
            public uint isVisible;           // XrBool32
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelStateEXT
        {
            public XrStructureType type;
            public IntPtr next;
            public uint nodeStateCount;
            public IntPtr nodeStates;        // XrRenderModelNodeStateEXT*
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelAssetCreateInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
            public XrUuid cacheId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelAssetDataGetInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelAssetDataEXT
        {
            public XrStructureType type;
            public IntPtr next;
            public uint bufferCapacityInput;
            public uint bufferCountOutput;
            public IntPtr buffer;            // uint8_t* (GLB data)
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelAssetPropertiesGetInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrRenderModelAssetPropertiesEXT
        {
            public XrStructureType type;
            public IntPtr next;
            public uint nodePropertyCount;
            public IntPtr nodeProperties;    // XrRenderModelAssetNodePropertiesEXT*
        }

        // Fixed 64 byte char array per node, read from unmanaged memory.
        private static class XrRenderModelAssetNodePropertiesEXT
        {
            public const int Size = 64;      // XR_MAX_RENDER_MODEL_ASSET_NODE_NAME_SIZE_EXT

            public static string ReadUniqueName(IntPtr nodeProperties)
            {
                byte[] buffer = new byte[Size];
                Marshal.Copy(nodeProperties, buffer, 0, Size);

                int length = Array.IndexOf(buffer, (byte)0);
                if (length < 0)
                {
                    length = Size;
                }

                return Encoding.UTF8.GetString(buffer, 0, length);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrInteractionRenderModelIdsEnumerateInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrInteractionRenderModelSubactionPathInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XrInteractionRenderModelTopLevelUserPathGetInfoEXT
        {
            public XrStructureType type;
            public IntPtr next;
            public uint topLevelUserPathCount;
            public IntPtr topLevelUserPaths;  // const XrPath*
        }

        #endregion

        #region OpenXR functions

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrLocateSpace(ulong space, ulong baseSpace, long time, ref XrSpaceLocation location);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrDestroySpace(ulong space);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrCreateRenderModelEXT(ulong session, ref XrRenderModelCreateInfoEXT createInfo, out ulong renderModel);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrDestroyRenderModelEXT(ulong renderModel);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrGetRenderModelPropertiesEXT(ulong renderModel, ref XrRenderModelPropertiesGetInfoEXT getInfo,
            ref XrRenderModelPropertiesEXT properties);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrCreateRenderModelSpaceEXT(ulong session, ref XrRenderModelSpaceCreateInfoEXT createInfo, out ulong space);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrGetRenderModelStateEXT(ulong renderModel, ref XrRenderModelStateGetInfoEXT getInfo, ref XrRenderModelStateEXT state);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrCreateRenderModelAssetEXT(ulong session, ref XrRenderModelAssetCreateInfoEXT createInfo, out ulong asset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrDestroyRenderModelAssetEXT(ulong asset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrGetRenderModelAssetDataEXT(ulong asset, ref XrRenderModelAssetDataGetInfoEXT getInfo,
            ref XrRenderModelAssetDataEXT buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrGetRenderModelAssetPropertiesEXT(ulong asset, ref XrRenderModelAssetPropertiesGetInfoEXT getInfo,
            ref XrRenderModelAssetPropertiesEXT properties);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrEnumerateInteractionRenderModelIdsEXT(ulong session, ref XrInteractionRenderModelIdsEnumerateInfoEXT enumerateInfo,
            uint renderModelIdCapacityInput, out uint renderModelIdCountOutput, [Out] ulong[] renderModelIds);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrEnumerateRenderModelSubactionPathsEXT(ulong renderModel, ref XrInteractionRenderModelSubactionPathInfoEXT info,
            uint pathCapacityInput, out uint pathCountOutput, [Out] ulong[] paths);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult PFN_xrGetRenderModelPoseTopLevelUserPathEXT(ulong renderModel, ref XrInteractionRenderModelTopLevelUserPathGetInfoEXT info,
            out ulong topLevelUserPath);

        #endregion
    }
}
