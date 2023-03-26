using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Chunk))]
public class Chunk_Inspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Regenerate"))
            {
                (target as Chunk).Regenerate();
            }
        }
    }
}
