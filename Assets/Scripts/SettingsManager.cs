using System;
using UnityEngine;
using UnityEngine.Assertions;

public class SettingsManager : MonoBehaviour
{
    private const string PLAYER_PREFS_VSYNC = "VSync";
    private const int PLAYER_PREFS_VSYNC_DEFAULT_VALUE = 1;

    private const string PLAYER_PREFS_DISPLAY_FPS_IN_MS = "DisplayFpsInMs";
    private const int PLAYER_PREFS_DISPLAY_FPS_IN_MS_DEFAULT_VALUE = 0;
    public static event Action<bool> OnDisplayFpsInMsChanged;

    private void Awake()
    {
        QualitySettings.vSyncCount = PlayerPrefs.GetInt(PLAYER_PREFS_VSYNC,
            PLAYER_PREFS_VSYNC_DEFAULT_VALUE);
    }

    public static void SetVSync(bool value)
    {
        QualitySettings.vSyncCount = Convert.ToInt32(value);
        PlayerPrefs.SetInt(PLAYER_PREFS_VSYNC, QualitySettings.vSyncCount);
    }

    public static int GetVSync()
    {
        return PlayerPrefs.GetInt(PLAYER_PREFS_VSYNC, PLAYER_PREFS_VSYNC_DEFAULT_VALUE);
    }

    public static void SetDisplayFpsInMs(bool value)
    {
        PlayerPrefs.SetInt(PLAYER_PREFS_DISPLAY_FPS_IN_MS, Convert.ToInt32(value));
        OnDisplayFpsInMsChanged?.Invoke(value);
    }

    public static bool GetDisplayFpsInMs()
    {
        return Convert.ToBoolean(PlayerPrefs.GetInt(PLAYER_PREFS_DISPLAY_FPS_IN_MS,
            PLAYER_PREFS_DISPLAY_FPS_IN_MS_DEFAULT_VALUE));
    }
}
