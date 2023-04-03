using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    private struct FPSSample
    {
        public float frameCount;
        public float sum;
        public float min;
        public float max;
    }

    [SerializeField]
    private TextMeshProUGUI FPSText;
    private FPSSample currentFPSSample;

    private void Start()
    {
        ResetCurrentFPSSample();
        StartCoroutine(UpdateFPSText());
    }

    private void Update()
    {
        UpdateCurrentFPSSample();
    }

    private IEnumerator UpdateFPSText(float interval = 1f)
    {
        WaitForSeconds waitForSeconds = new(interval);
        while (true)
        {
            yield return waitForSeconds;
            FPSText.SetText("{0:0} FPS (\u2193{1:0} \u2191{2:0})",
                currentFPSSample.sum / currentFPSSample.frameCount, currentFPSSample.min, currentFPSSample.max);
            ResetCurrentFPSSample();
        }
    }

    private void ResetCurrentFPSSample()
    {
        currentFPSSample.frameCount = 0;
        currentFPSSample.sum = 0;
        currentFPSSample.min = float.MaxValue;
        currentFPSSample.max = float.MinValue;
    }

    private void UpdateCurrentFPSSample()
    {
        float FPS = 1f / Time.unscaledDeltaTime;
        currentFPSSample.frameCount++;
        currentFPSSample.sum += FPS;
        if (FPS < currentFPSSample.min)
        {
            currentFPSSample.min = FPS;
        }
        if (FPS > currentFPSSample.max)
        {
            currentFPSSample.max = FPS;
        }
    }
}
