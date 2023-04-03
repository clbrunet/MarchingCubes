using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI FPSText;

    private void Start()
    {
        StartCoroutine(UpdateFPSText());
    }

    private IEnumerator UpdateFPSText(float interval = 0.1f)
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(interval);
        while (true)
        {
            FPSText.text = (int)(1f / Time.unscaledDeltaTime) + " FPS";
            yield return waitForSeconds;
        }
    }
}
