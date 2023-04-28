using System;
using UnityEngine;
using UnityEngine.Assertions;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private bool isGamePaused = false;

    public event Action<bool> OnPauseStateChanged;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isGamePaused = !isGamePaused;
            Time.timeScale = isGamePaused ? 0f : 1f;
            OnPauseStateChanged?.Invoke(isGamePaused);
        }
    }

    public bool IsGamePaused()
    {
        return isGamePaused;
    }
}
