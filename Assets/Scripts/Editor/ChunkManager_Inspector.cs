using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChunkManager))]
public class ChunkManager_Inspector : Editor
{
    private static bool autoReloadChunks;

    public override void OnInspectorGUI()
    {
        bool defaultInspector = DrawDefaultInspector();
        if (!EditorApplication.isPlaying)
        {
            return;
        }
        autoReloadChunks = EditorGUILayout.Toggle("Auto reload chunks", autoReloadChunks);
        bool reloadButton = false;
        if (!autoReloadChunks)
        {
            reloadButton = GUILayout.Button("Reload chunks");
        }
        if ((autoReloadChunks && defaultInspector) || reloadButton)
        {
            (target as ChunkManager).ReloadChunks();
        }
    }
}
