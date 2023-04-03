using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [SerializeField]
    private Toggle vSyncToggle;
    [SerializeField]
    private Button quitButton;

    private void Awake()
    {
        vSyncToggle.onValueChanged.AddListener((bool value) =>
        {
            QualitySettings.vSyncCount = (value) ? 1 : 0;
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
        vSyncToggle.SetIsOnWithoutNotify(QualitySettings.vSyncCount != 0);
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
