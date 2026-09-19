using UnityEngine;

namespace Valve.OpenXR.Utils
{
    /// <summary>
    /// Drop this on your tracking-space transform (the parent of the tracked camera; the thing your locomotion code
    /// moves) and it registers itself with <see cref="ValveOpenXRAppSpaceDeltaPoseFeature"/>. The first Camera found
    /// underneath supplies the depth range; Camera.main is used if there is none.
    /// </summary>
    [AddComponentMenu("XR/Valve/App Space Delta Pose Tracking Space")]
    [DisallowMultipleComponent]
    public class AppSpaceDeltaPoseTrackingSpace : MonoBehaviour
    {
        private void OnEnable()
        {
            ValveOpenXRAppSpaceDeltaPoseFeature.Instance?.SetTrackingSpace(transform, GetComponentInChildren<Camera>());
        }

        private void OnDisable()
        {
            ValveOpenXRAppSpaceDeltaPoseFeature.Instance?.SetTrackingSpace(null);
        }
    }
}
