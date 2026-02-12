using UnityEngine;
using Valve.OpenXR.Utils;

public class SteamFrameCheck : MonoBehaviour
{
    enum CheckState { Unknown, Yes, No }
    
    [SerializeField]
    private GameObject[] _states;
    
    private bool _firstTime = true;

    private void Awake()
    {
        SetToggleState(CheckState.Unknown);
    }
    
    private void Update()
    {
        if (_firstTime)
        {
            if (SystemInfoFeature.IsInitialized())
            {
                bool isRunningOnFrame = SystemInfoFeature.IsRunnginOnSteamFrame();
                SetToggleState(isRunningOnFrame ? CheckState.Yes : CheckState.No);
                _firstTime = false;
            }
        }
    }

    private void SetToggleState(CheckState state)
    {
        var stateIndex = (int)state;

        if (stateIndex < _states.Length)
        {
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i].gameObject.SetActive(i == stateIndex);
            }
        }
    }
}
