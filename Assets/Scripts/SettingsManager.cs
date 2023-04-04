using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string PLAYER_PREFS_VSYNC = "VSync";
    private const int PLAYER_PREFS_VSYNC_DEFAULT_VALUE = 1;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    private void Start()
    {
        QualitySettings.vSyncCount = PlayerPrefs.GetInt(PLAYER_PREFS_VSYNC, PLAYER_PREFS_VSYNC_DEFAULT_VALUE);
    }

    public void SetVSync(bool value)
    {
        QualitySettings.vSyncCount = (value) ? 1 : 0;
        PlayerPrefs.SetInt(PLAYER_PREFS_VSYNC, QualitySettings.vSyncCount);
    }
}
