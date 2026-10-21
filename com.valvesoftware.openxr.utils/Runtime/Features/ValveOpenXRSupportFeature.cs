using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Rendering settings that Unity's OpenXR plugin reads at build time: buffer discard optimisation and late latching.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: Settings for Unity's Rendering",
        Desc = "Settings for OpenXR Rendering.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        Version = "0.1.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
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

#if UNITY_EDITOR
        internal void ApplySettingsOverride(OpenXRSettings openXrSettings)
        {
#if UNITY_ANDROID
            openXrSettings.optimizeBufferDiscards = optimizeBufferDiscards;
#endif
        }

        protected override void GetValidationChecks(List<ValidationRule> rules, BuildTargetGroup targetGroup)
        {
#if UNITY_ANDROID
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
#endif
        }
#endif
    }
}
