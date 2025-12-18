#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;

namespace Valve.OpenXR.Utils.Editor
{
    [OpenXRFeatureSet(
        UiName = "Valve Utils",
        Description = "Collection of useful resource to assist with OpenXR development",
        FeatureSetId = featureSetId,
        SupportedBuildTargets = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
        FeatureIds = new [] {
            ValveOpenXRSupportFeature.featureId,
            ValveOpenXRFoveatedRenderingFeature.featureId,
            ValveOpenXRRenderRegionsFeature.featureId,
            ValveOpenXRLeptonValidationFeature.featureId,
            ValveOpenXRRefreshRateFeature.featureId
            },
        DefaultFeatureIds = new string[0]
    )]
    sealed class ValveOpenXRFeatureSet
    {
        public const string featureSetId = "com.valvesoftware.openxr.utils.featureset";
    }
}
#endif
