using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR;

public class RefreshRateFeatureExample : MonoBehaviour
{
    ValveOpenXRRefreshRateFeature feature;
    TextMesh textMesh;

    private void Start()
    {
        textMesh = GetComponent<TextMesh>();
        feature = OpenXRSettings.Instance.GetFeature<ValveOpenXRRefreshRateFeature>();
    }

    private void Update()
    {
        string message = "Unknown error";

        if (!feature || !feature.enabled)
        {
            message = "RefreshRateFeature feature not enabled";
        }
        else if (feature.initialized == false)
        {
            message = "RefreshRate.initialized == false.";
        }
        else
        {
            List<float> displayRefreshRates = new List<float>();
            feature.EnumerateRefreshRates(ref displayRefreshRates); 
            float refreshrate = feature.GetRefreshRate();
            message = $"Refresh Rates: [{string.Join(",", displayRefreshRates)}]  Current: {refreshrate}";
        }

        if (textMesh && !textMesh.text.Equals(message))
        {
            textMesh.text = message;
            Debug.Log(message);
        }
    }
}