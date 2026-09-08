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
            if (SystemInfoFeature.IsInitialized())
            {
                bool isRunningOnFrame = SystemInfoFeature.IsRunnginOnSteamFrame();
                Debug.Log($"Is running on Steam Frame? {(isRunningOnFrame?"y":"n")}");
                done = true;
            }

            yield return null;
        }
    }
}
