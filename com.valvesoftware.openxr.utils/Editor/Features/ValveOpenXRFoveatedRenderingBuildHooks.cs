using System;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;

namespace Valve.OpenXR.Utils.Editor
{
    internal class ValveOpenXRFoveatedRenderingBuildHooks : OpenXRFeatureBuildHooks
    {
        public override int callbackOrder => 1;
        public override Type featureType => typeof(ValveOpenXRFoveatedRenderingFeature);

        protected override void OnPostGenerateGradleAndroidProjectExt(string path) {}
        protected override void OnPostprocessBuildExt(BuildReport report) {}
        protected override void OnPreprocessBuildExt(BuildReport report) {}

        protected override void OnProcessBootConfigExt(BuildReport report, BootConfigBuilder builder)
        {
#if !USE_LEGACY_BOOT_CONFIG
            builder.SetBootConfigValue("xr-vulkan-extension-fragment-density-map-enabled", "1");
#else
            builder.SetBootConfigValue("xr-meta-enabled", "1");
#endif
        }
    }
}