using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace Valve.OpenXR.Utils.Editor
{
    internal class ValveOpenXRRenderRegionsFeatureBuildHooks : OpenXRFeatureBuildHooks
    {
        private const string kMvpvvEnabled = "xr-mvpvv-enabled";

        public override int callbackOrder => 2;

        public override Type featureType => typeof(ValveOpenXRRenderRegionsFeature);

        protected override void OnPreprocessBuildExt(BuildReport report)
        {
            ApplySettingsOverride();
        }

        protected override void OnProcessBootConfigExt(BuildReport report, BootConfigBuilder builder)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            var item = EditorUtils.GetFeatureAsset<ValveOpenXRRenderRegionsFeature>();
            if (item == null)
            {
                Debug.Log("Unable to locate the render regions feature asset");
                return;
            }

            // Update the boot config
#if UNITY_6000_1_OR_NEWER
            builder.SetBootConfigValue(kMvpvvEnabled, ((int)item.multiviewRenderRegionsOptimizationMode).ToString());
#endif
        }

        protected override void OnPostGenerateGradleAndroidProjectExt(string path)
        {
        }

        protected override void OnPostprocessBuildExt(BuildReport report)
        {
        }

        private void ApplySettingsOverride()
        {
            var openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var target = EditorUtils.GetFeatureAsset<ValveOpenXRRenderRegionsFeature>();
            target.ApplySettingsOverride(openXrSettings);
            AssetDatabase.SaveAssetIfDirty(openXrSettings);
        }
    }
}
