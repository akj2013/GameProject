using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexGridManager))]
public class HexGridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HexGridManager grid = (HexGridManager)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Grid"))
        {
            grid.GenerateGrid();
        }

        if (GUILayout.Button("Clear Grid"))
        {
            grid.ClearGridImmediate();
        }
    }
}