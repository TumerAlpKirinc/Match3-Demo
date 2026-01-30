using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelData))]
public class LevelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); 
        LevelData data = (LevelData)target;

        data.InitializeLayout();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Harita Tasarýmý (0: Boþ, 1: Meyve, 2: Kutu)", EditorStyles.boldLabel);

        // Izgarayý çiz
        for (int y = data.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < data.width; x++)
            {
                int index = y * data.width + x;
                int val = data.boardLayout[index];

                // Renklendirme
                if (val == 2) GUI.backgroundColor = Color.black;      
                else if (val == 1) GUI.backgroundColor = Color.green; 
                else GUI.backgroundColor = Color.gray;

                if (GUILayout.Button(val.ToString(), GUILayout.Width(30), GUILayout.Height(30)))
                {
                    data.boardLayout[index] = (val + 1) % 3;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
        if (GUI.changed) EditorUtility.SetDirty(data);
    }
}