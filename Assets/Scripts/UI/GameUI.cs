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

    private void Start()
    {
        FPSText.text = "";
        ResetCurrentFPSSample();
        InvokeRepeating(nameof(UpdateFPSText), 0.1f, 0.5f);
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
        FPSText.SetText("{0:0} FPS (\u2193{1:0} \u2191{2:0})",
            frameDurationsSample.count / frameDurationsSample.sum,
            1f / frameDurationsSample.min, 1f / frameDurationsSample.max);
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
