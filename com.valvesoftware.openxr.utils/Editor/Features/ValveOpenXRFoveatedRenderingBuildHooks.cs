using System;
using System.Collections.Generic;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;

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
        
        protected override ManifestRequirement ProvideManifestRequirementExt()
        {
            var elementsToRemove = new List<ManifestElement>()
            {
            };
            
            var elementsToAdd = new List<ManifestElement>()
            {
                new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "uses-feature" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "oculus.software.eye_tracking" },
                        { "required", "true" }
                    }
                },
                
                new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "uses-permission" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "com.oculus.permission.EYE_TRACKING" }
                    }
                },
                
                new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "uses-permission" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "android.permission.EYE_TRACKING" }
                        
                    }
                },
                
                new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "uses-permission" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "android.permission.EYE_TRACKING_FINE" }
                    }
                }
            };
            
            return new ManifestRequirement
            {
                SupportedXRLoaders = new HashSet<Type>()
                {
                    typeof(OpenXRLoader)
                },
                NewElements = elementsToAdd,
                RemoveElements = elementsToRemove
            };
        }
    }
}