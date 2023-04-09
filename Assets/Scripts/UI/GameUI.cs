using System.Collections;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    private struct FrameDurationsSample
    {
        public float count;
        public float sum;
        public float min;
        public float max;
    }

    [SerializeField]
    private TextMeshProUGUI FPSText;
    private FrameDurationsSample frameDurationsSample;
    private bool displayFpsInMs;

    private void Awake()
    {
        FPSText.text = "";
        ResetCurrentFPSSample();
        displayFpsInMs = SettingsManager.GetDisplayFpsInMs();
        SettingsManager.OnDisplayFpsInMsChanged += SettingsManager_OnDisplayFpsInMsChanged;
    }

    private void Start()
    {
        InvokeRepeating(nameof(UpdateFPSText), 0.5f, 0.5f);
    }

    private void OnDestroy()
    {
        SettingsManager.OnDisplayFpsInMsChanged -= SettingsManager_OnDisplayFpsInMsChanged;
    }

    private void SettingsManager_OnDisplayFpsInMsChanged(bool value)
    {
        displayFpsInMs = value;
    }

    private void Update()
    {
        UpdateCurrentFPSSample();
    }

    private void UpdateCurrentFPSSample()
    {
        float frameDuration = Time.unscaledDeltaTime;
        frameDurationsSample.count++;
        frameDurationsSample.sum += frameDuration;
        if (frameDuration < frameDurationsSample.min)
        {
            frameDurationsSample.min = frameDuration;
        }
        if (frameDuration > frameDurationsSample.max)
        {
            frameDurationsSample.max = frameDuration;
        }
    }

    private void UpdateFPSText()
    {
        if (frameDurationsSample.count == 0)
        {
            FPSText.text = "0 FPS";
            return;
        }
        if (displayFpsInMs)
        {
            FPSText.SetText("{0:1} MS (\u2193{1:1} \u2191{2:1})",
                frameDurationsSample.sum / frameDurationsSample.count * 1000,
                frameDurationsSample.max * 1000, frameDurationsSample.min * 1000);
        }
        else
        {
            FPSText.SetText("{0:0} FPS (\u2193{1:0} \u2191{2:0})",
                frameDurationsSample.count / frameDurationsSample.sum,
                1f / frameDurationsSample.max, 1f / frameDurationsSample.min);
        }
        ResetCurrentFPSSample();
    }

    private void ResetCurrentFPSSample()
    {
        frameDurationsSample.count = 0;
        frameDurationsSample.sum = 0;
        frameDurationsSample.min = float.MaxValue;
        frameDurationsSample.max = float.MinValue;
    }
}
