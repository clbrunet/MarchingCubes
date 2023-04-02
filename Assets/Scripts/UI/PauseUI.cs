using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [SerializeField]
    private Button quitButton;

    private void Awake()
    {
        quitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        });
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
