using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR.Features;

namespace Valve.OpenXR.Utils
{
#if UNITY_EDITOR
    public static class EditorUtils
    {
        internal static T GetFeatureAsset<T>() where T : OpenXRFeature
        {
            var featureGuids = AssetDatabase.FindAssets("t:" + typeof(T).Name);

            if (featureGuids.Length != 1)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(featureGuids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        internal static bool UsesVulkan(BuildTarget buildTarget)
        {
            BuildTarget[] targetsThatDefaultToVulkan = {BuildTarget.Android};
            if (!PlayerSettings.GetUseDefaultGraphicsAPIs(buildTarget))
            {
                GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                if (apis.Length >= 1 && apis[0] == GraphicsDeviceType.Vulkan)
                {
                    return true;
                }
                return false;
            }
            return targetsThatDefaultToVulkan.Contains(buildTarget);
        }
    }
#endif
}