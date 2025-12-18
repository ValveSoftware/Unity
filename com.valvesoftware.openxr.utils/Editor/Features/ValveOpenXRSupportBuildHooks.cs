using System;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;

namespace Valve.OpenXR.Utils.Editor
{
    internal class ValveOpenXRSupportBuildHooks : OpenXRFeatureBuildHooks
    {
        public override int callbackOrder => 1;
        public override Type featureType => typeof(ValveOpenXRSupportFeature);

        private const string kLateLatchingSupported = "xr-latelatching-enabled";
        private const string kLateLatchingDebug = "xr-latelatchingdebug-enabled";

        protected override void OnPostGenerateGradleAndroidProjectExt(string path) {}
        protected override void OnPostprocessBuildExt(BuildReport report) {}
        protected override void OnPreprocessBuildExt(BuildReport report) {}

        protected override void OnProcessBootConfigExt(BuildReport report, BootConfigBuilder builder)
        {
            var item = EditorUtils.GetFeatureAsset<ValveOpenXRSupportFeature>();
            if (item == null)
            {
                Debug.Log("Unable to locate the OpenXR support feature asset");
                return;
            }

            builder.SetBootConfigValue(kLateLatchingSupported, item.lateLatchingMode ? "1" : "0");
            builder.SetBootConfigValue(kLateLatchingDebug, item.lateLatchingDebug ? "1" : "0");
        }
    }
}