
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif
using Object = UnityEngine.Object;

[assembly: InternalsVisibleTo("Valve.OpenXR.Utils.Editor")]
namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Support for Unity's Multiview Render Regions.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: Settings for Unity's Render Regions",
        Desc = "Configuration of Unity's Render Regions.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        OpenxrExtensionStrings = extensionStrings,
        Version = "0.1.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        FeatureId = featureId
    )]
#endif
    public class ValveOpenXRRenderRegionsFeature : OpenXRFeature
    {
        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>
        public const string featureId = "com.valvesoftware.openxr.utils.render_regions";

        private const string extensionStrings = "";

        /// <summary>OnBeforeSerialize.</summary>
        public void OnBeforeSerialize()
        {
#if UNITY_6000_1_OR_NEWER && UNITY_EDITOR
#pragma warning disable CS0618 // suppress obsolete field usage warning
            optimizeMultiviewRenderRegions = multiviewRenderRegionsOptimizationMode != OpenXRSettings.MultiviewRenderRegionsOptimizationMode.None;
#pragma warning restore CS0618
#endif
        }
        /// <summary>OnAfterDeserialize.</summary>
        public void OnAfterDeserialize()
        {
#if UNITY_6000_1_OR_NEWER && UNITY_EDITOR
            if (!m_hasMigratedMultiviewRenderRegions)
            {
#pragma warning disable CS0618 // suppress obsolete field usage warning
                if (optimizeMultiviewRenderRegions)
                    multiviewRenderRegionsOptimizationMode = OpenXRSettings.MultiviewRenderRegionsOptimizationMode.FinalPass;
                else
                    multiviewRenderRegionsOptimizationMode = OpenXRSettings.MultiviewRenderRegionsOptimizationMode.None;
#pragma warning restore CS0618

                m_hasMigratedMultiviewRenderRegions = true;
            }
#endif
        }
        
        
#if UNITY_EDITOR

        [SerializeField]
        internal bool symmetricProjection = false;

        /// <summary>
        /// If enabled, the application can use Multi-View Per View Viewports functionality. This feature requires Unity 6.1 or later, and usage of the Vulkan renderer.
        /// </summary>
        [SerializeField, HideInInspector]
        [Obsolete("optimizeMultiviewRenderRegions is deprecated. Use multiviewRenderRegionsOptimizationMode instead.", false)]
        internal bool optimizeMultiviewRenderRegions;

#if UNITY_6000_1_OR_NEWER
        /// <summary>
        /// Selected Multiview Render Region Optimization Mode. This feature requires Unity 6.1 or later, and usage of the Vulkan renderer.
        /// </summary>
        [SerializeField]
        internal OpenXRSettings.MultiviewRenderRegionsOptimizationMode multiviewRenderRegionsOptimizationMode;

        [SerializeField, HideInInspector]
        private bool m_hasMigratedMultiviewRenderRegions = false;
#endif
        
        private bool SettingsUseVulkan()
        {
            if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android))
            {
                GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                if (apis.Length >= 1 && apis[0] == GraphicsDeviceType.Vulkan)
                {
                    return true;
                }
                return false;
            }

            return true;
        }


        private ValidationRule[] CreateValidationRules(BuildTargetGroup targetGroup) =>

            new ValidationRule[]
            {
                    // OptimizeMultiviewRenderRegions (aka MVPVV) only supported on Unity 6.1 onwards
#if UNITY_6000_1_OR_NEWER
                    new ValidationRule(this)
                    {
                        message = "Multiview Render Regions Optimizations Mode requires symmetric projection setting turned on.",
                        checkPredicate = () =>
                        {
                            if (multiviewRenderRegionsOptimizationMode != OpenXRSettings.MultiviewRenderRegionsOptimizationMode.None)
                            {
                                return symmetricProjection;
                            }
                            return true;
                        },
                        error = true,
                        fixIt = () =>
                        {
                            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                            var feature = settings.GetFeature<ValveOpenXRRenderRegionsFeature>();
                            feature.symmetricProjection = true;
                        }
                    },

                    new ValidationRule(this)
                    {
                        message = "Multiview Render Regions Optimizations Mode requires Render Mode set to \"Single Pass Instanced / Multi-view\".",
                        checkPredicate = () =>
                        {
                            if (multiviewRenderRegionsOptimizationMode != OpenXRSettings.MultiviewRenderRegionsOptimizationMode.None)
                            {
                                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                                return (settings.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced);
                            }
                            return true;
                        },
                        error = true,
                        fixIt = () =>
                        {
                            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                            settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                        }
                    },

                    new ValidationRule(this)
                    {
                        message = "Multiview Render Regions Optimizations Mode needs the Vulkan Graphics API to be the default Graphics API to work at runtime.",
                        helpText = "The Multiview Render Regions Optimizations Mode feature only works with the Vulkan Graphics API, which needs to be set as the first Graphics API to be loaded at application startup. Choosing other Graphics API may require to switch to Vulkan and restart the application.",
                        checkPredicate = () =>
                        {
                            if (multiviewRenderRegionsOptimizationMode != OpenXRSettings.MultiviewRenderRegionsOptimizationMode.None)
                            {
                                var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                                return graphicsApis[0] == GraphicsDeviceType.Vulkan;
                            }
                            return true;
                        },
                        error = false
                    },

                    new ValidationRule(this)
                    {
                        message = "Multiview Render Regions Optimizations - All Passes mode is only supported on Unity 6.2+ versions",
                        checkPredicate = () =>
                        {
#if !UNITY_6000_2_OR_NEWER
                            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                            if (settings.multiviewRenderRegionsOptimizationMode == OpenXRSettings.MultiviewRenderRegionsOptimizationMode.AllPasses)
                                return false;
#endif
                            return true;
                        },
                        error = true,
                        fixIt = () =>
                        {
                            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                            var feature = settings.GetFeature<ValveOpenXRRenderRegionsFeature>();
                            feature.multiviewRenderRegionsOptimizationMode = OpenXRSettings.MultiviewRenderRegionsOptimizationMode.FinalPass;
                        },
                        fixItAutomatic = true,
                        fixItMessage = "Set Multiview Render Regions Optimization Mode to Final Pass."
                    },

#endif

#if UNITY_ANDROID
                    new ValidationRule(this)
                    {
                        message = "Symmetric Projection is only supported on Vulkan graphics API",
                        checkPredicate = () =>
                        {
                            if (symmetricProjection && !SettingsUseVulkan())
                            {
                                return false;
                            }
                            return true;
                        },
                        fixIt = () =>
                        {
                            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
                        },
                        fixItAutomatic = true,
                        fixItMessage = "Set Vulkan as Graphics API"
                    },

                    new ValidationRule(this)
                    {
                        message = "Symmetric Projection is only supported when using Multi-view",
                        checkPredicate = () =>
                        {
                            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                            if (null == settings)
                                return false;

                            if (symmetricProjection && (settings.renderMode != OpenXRSettings.RenderMode.SinglePassInstanced))
                            {
                                return false;
                            }
                            return true;
                        },
                        fixIt = () =>
                        {
                            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                            if (null != settings)
                            {
                                settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                            }
                        },
                        error = true,
                        fixItAutomatic = true,
                        fixItMessage = "Set Render Mode to Multi-view"
                    },

#endif
            };

        internal class ValveOpenXRRenderRegionsFeatureEditorWindow : EditorWindow
        {
            private Object feature;
            private Editor featureEditor;
    
            public static EditorWindow Create(Object feature)
            {
                var window = EditorWindow.GetWindow<ValveOpenXRRenderRegionsFeatureEditorWindow>(true, "Valve OpenXR Render Region Configuration", true);
                window.feature = feature;
                window.featureEditor = Editor.CreateEditor(feature);
                return window;
            }

            private void OnGUI() 
            {
                featureEditor.OnInspectorGUI();
            }
        }
#endif
    }
}
