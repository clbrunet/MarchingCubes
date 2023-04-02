using System;
using System.Collections;
using System.Collections.Generic;
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
            OnPauseStateChanged?.Invoke(isGamePaused);
        }
    }

    public bool IsGamePaused()
    {
        return isGamePaused;
    }
}
