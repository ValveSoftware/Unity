using System.Collections.Generic;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using UnityEngine.XR.OpenXR;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Enables the Valve OpenXR Loader support.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Valve Utils: Lepton Validation",
        Desc = "Validation checks for Android.",
        Company = "Valve Software",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#open-xr-features",
        OpenxrExtensionStrings = "",
        Version = "0.1.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        FeatureId = featureId
    )]
#endif
    public class ValveOpenXRLeptonValidationFeature : OpenXRFeature
    {
        public const string featureId = "com.valvesoftware.openxr.utils.validation";

        private System.Type[] incompatibleFeatureTypes = { typeof(MetaQuestFeature) };
        
#if UNITY_EDITOR
        protected override void GetValidationChecks(List<ValidationRule> rules, BuildTargetGroup targetGroup)
        {
            base.GetValidationChecks(rules, targetGroup);

            var highestMinAndroidApiLevel = AndroidSdkVersions.AndroidApiLevel30;
            rules.Add(new ValidationRule(this)
            {
                message = "Minimum supported Android API level must be <= 30.",
                checkPredicate = () =>
                {
                    var minApi = PlayerSettings.Android.minSdkVersion;
                    return minApi <= highestMinAndroidApiLevel;
                },
                error = true,
                fixIt = () => { PlayerSettings.Android.minSdkVersion = highestMinAndroidApiLevel; },
                fixItAutomatic = true,
                fixItMessage = "Open Project Settings to select a minimum API level."
            });
            
            rules.Add(new ValidationRule(this)
            {
                message = "Valve OpenXR on Android requires IL2CPP compatibility.",
                checkPredicate = () =>
                {
#if UNITY_2023_1_OR_NEWER
                    var backend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup));
#else
                    var backend = PlayerSettings.GetScriptingBackend(targetGroup);
#endif
                    return backend == ScriptingImplementation.IL2CPP;
                },
                error = true,
                fixIt = () =>
                {
#if UNITY_2023_1_OR_NEWER
                    PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup), ScriptingImplementation.IL2CPP);
#else
                    PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.IL2CPP);
#endif
                },
                fixItAutomatic = true,
                fixItMessage = "Open Project Settings to set the scripting backend."
            });
            
            rules.Add(new ValidationRule(this)
            {
                message = "Valve OpenXR on Android requires ARM64 only",
                checkPredicate = () => PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64,
                error = true,
                fixIt = () =>
                {
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                },
                fixItMessage = "Set target architecture to ARM64 only."
            });
            
            rules.Add(new ValidationRule(this)
            {
                message = "Valve OpenXR is not compatible with other built-in features for other XR platforms.",
                checkPredicate = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    if (!settings)
                        return true;

                    foreach (var t in incompatibleFeatureTypes)
                    {
                        var f = settings.GetFeature(t);
                        if (f && f.enabled)
                            return false;
                    }

                    return true;
                },
                error = true,
                fixIt = () =>
                {
                    var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                    if (settings)
                    {
                        foreach (var t in incompatibleFeatureTypes)
                        {
                            var feature = settings.GetFeature(t);
                            if (feature)
                            {
                                feature.enabled = false;
                            }
                        }
                    }
                },
                fixItAutomatic = true,
                fixItMessage = "Disable incompatible features."
            });
        }
#endif
    }
}
