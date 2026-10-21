using System.Collections;
using UnityEngine;
using Valve.OpenXR.Utils;

public class SteamFrameCheckSimple : MonoBehaviour
{
    private IEnumerator Start()
    {
        bool done = false;

        while (!done)
        {
            if (ValveOpenXRSystemInfoFeature.IsInitialized())
            {
                string systemName = ValveOpenXRSystemInfoFeature.GetSystemName();
                bool isLocalSteamFrame = ValveOpenXRSystemInfoFeature.IsLocalSteamFrame();
                Debug.Log($"OpenXR System: {systemName}, is local Steam Frame? {isLocalSteamFrame}");
                done = true;
            }

            yield return null;
        }
    }
}
