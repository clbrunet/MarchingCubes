using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [SerializeField]
    private Toggle vSyncToggle;
    [SerializeField]
    private Toggle displayFpsInMs;
    [SerializeField]
    private Button quitButton;

    private void Awake()
    {
        vSyncToggle.onValueChanged.AddListener((bool value) =>
        {
            SettingsManager.SetVSync(value);
        });
        displayFpsInMs.onValueChanged.AddListener((bool value) =>
        {
            SettingsManager.SetDisplayFpsInMs(value);
        });
        quitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        });
    }

    private void OnEnable()
    {
        vSyncToggle.SetIsOnWithoutNotify(Convert.ToBoolean(SettingsManager.GetVSync()));
        displayFpsInMs.SetIsOnWithoutNotify(SettingsManager.GetDisplayFpsInMs());
    }

    private void Start()
    {
        GameManager.Instance.OnPauseStateChanged += GameManager_OnPauseStateChanged;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnPauseStateChanged -= GameManager_OnPauseStateChanged;
    }

    private void GameManager_OnPauseStateChanged(bool isGamePaused)
    {
        gameObject.SetActive(isGamePaused);
    }
}
