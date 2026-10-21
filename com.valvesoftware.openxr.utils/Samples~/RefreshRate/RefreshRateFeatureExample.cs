using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using Valve.OpenXR.Utils;

public class RefreshRateFeatureExample : MonoBehaviour
{
    ValveOpenXRRefreshRateFeature feature;
    TextMesh textMesh;
    // Supported rates are formatted once per session. Only the current rate is polled.
    List<float> displayRefreshRates = new List<float>();
    string displayRefreshRatesText;
    float shownRefreshRate;
    string message;

    private void Start()
    {
        textMesh = GetComponent<TextMesh>();
        feature = OpenXRSettings.Instance.GetFeature<ValveOpenXRRefreshRateFeature>();
    }

    private void Update()
    {
        if (!feature || !feature.enabled)
        {
            displayRefreshRatesText = null;
            ShowMessage("RefreshRateFeature feature not enabled");
        }
        else if (feature.initialized == false)
        {
            displayRefreshRatesText = null;
            ShowMessage("RefreshRate.initialized == false.");
        }
        else
        {
            if (displayRefreshRatesText == null)
            {
                feature.EnumerateRefreshRates(ref displayRefreshRates);
                displayRefreshRatesText = string.Join(",", displayRefreshRates);
                shownRefreshRate = float.NaN;
            }

            float refreshrate = feature.GetRefreshRate();
            if (refreshrate != shownRefreshRate)
            {
                shownRefreshRate = refreshrate;
                ShowMessage($"Refresh Rates: [{displayRefreshRatesText}]  Current: {refreshrate}");
            }
        }
    }

    private void ShowMessage(string newMessage)
    {
        // TextMesh.text allocates on read, so compare against the last message.
        if (newMessage == message)
        {
            return;
        }

        message = newMessage;
        if (textMesh)
        {
            textMesh.text = message;
            Debug.Log(message);
        }
    }
}
