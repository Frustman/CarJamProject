using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LevelEditor : EditorWindow
{
    private static LevelEditor Window;
    private static float width = 500;
    private static float height = 600;

    private Editor levelEditor;
    private LevelData[] levelData;
    private string[] levelDataOptions;

    private int selectedLevelIndex = 0;
    private int loadedLevelCount = 0;

    private int lastSelectedIndex = -1;

    private Vector2 scrollPosition;

    [MenuItem("LevelEditor/Level")]
    public static void Open()
    {
        Window = GetWindow<LevelEditor>();

        Rect mainRect = EditorGUIUtility.GetMainWindowPosition();
        Window.minSize = new Vector2(width, height);
    }

    private void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        if (GUILayout.Button("Load All Levels in assets"))
        {
            LoadLevelData();
        }

        if (loadedLevelCount > 0)
        {

            selectedLevelIndex = EditorGUILayout.Popup(selectedIndex: selectedLevelIndex, displayedOptions: levelDataOptions);

            if (lastSelectedIndex != selectedLevelIndex)
            {
                levelEditor = Editor.CreateEditor(levelData[selectedLevelIndex]);
                lastSelectedIndex = selectedLevelIndex;
            }

            if (levelEditor)
            {
                EditorGUILayout.Space(20f);

                levelEditor.OnInspectorGUI();

            }
        }

        GUILayout.EndScrollView();
    }


    private void LoadLevelData()
    {
        levelData = Resources.LoadAll<LevelData>("Levels/");
        levelDataOptions = new string[levelData.Length];

        loadedLevelCount = levelData.Length;
        for (int i = 0; i < levelData.Length; i++)
        {
            levelDataOptions[i] = levelData[i].name;
        }
        lastSelectedIndex = -1;
    }
}
