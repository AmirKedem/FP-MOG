using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor;

public class LaunchWindow : EditorWindow
{
    Data data = new Data();
    Vector2 scrollPosition;

    [Serializable]
    class Entry
    {
        public bool selected;
        public bool runInEditor;
        public int count = 1;
        public BuildUtils.GameLoopMode name = BuildUtils.GameLoopMode.Client;
    }

    [Serializable]
    class Data
    {
        public List<Entry> entries = new List<Entry>();
    }

    [MenuItem("Project Managment/Windows/Launch Tools")]
    public static void ShowWindow()
    {
        GetWindow<LaunchWindow>(false, "Launch Tools", true);
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        var defaultGUIColor = GUI.color;
        var defaultGUIBackgrounColor = GUI.backgroundColor;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical();

        // Quick start buttons
        DrawLine();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Start Selected"))
        {
            for (var i = 0; i < data.entries.Count; i++)
            {
                var entry = data.entries[i];
                if (!entry.selected)
                    continue;
                StartEntry(data.entries[i]);
            }
        }
        GUI.backgroundColor = defaultGUIBackgrounColor;

        DrawLine();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Stop All"))
        {
            BuildUtils.StopAll();
        }
        GUI.backgroundColor = defaultGUIBackgrounColor;

        DrawLine();
        if (GUILayout.Button("Add Entry"))
        {
            data.entries.Add(new Entry());
        }
        DrawLine();
        GUILayout.Space(10.0f);
        
        // Draw entries
        for (int i = 0; i < data.entries.Count; i++)
        {
            GUILayout.Space(5.0f);
            var entry = data.entries[i];

            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            {
                GUILayout.Space(5.0f);
                GUI.backgroundColor = entry.selected ? Color.green : defaultGUIBackgrounColor;
                if (GUILayout.Button("S", GUILayout.Width(20)))
                    entry.selected = !entry.selected;
                GUI.backgroundColor = defaultGUIBackgrounColor;

                entry.name = (BuildUtils.GameLoopMode)EditorGUILayout.EnumPopup(entry.name);

                GUILayout.Label("Count:", GUILayout.Width(40));
                entry.count = EditorGUILayout.IntField(entry.count, GUILayout.Width(40), GUILayout.ExpandWidth(false));

                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Start", GUILayout.Width(50)))
                {
                    StartEntry(entry);
                }
                GUI.backgroundColor = defaultGUIBackgrounColor;
                GUI.backgroundColor = entry.runInEditor ? Color.yellow : GUI.backgroundColor;
                GUI.backgroundColor = defaultGUIBackgrounColor;

                var runInEditor = GUILayout.Toggle(entry.runInEditor, "Editor", new GUIStyle("Button"), GUILayout.Width(50));
                if (runInEditor != entry.runInEditor)
                {
                    for (var j = 0; j < data.entries.Count; j++)
                        data.entries[j].runInEditor = false;
                    entry.runInEditor = runInEditor;
                }
                GUILayout.Space(5.0f);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.FlexibleSpace();

        GUILayout.Space(10.0f);

        DrawLine();

        // Remove All Entries button
        GUILayout.BeginHorizontal();
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove All Entries"))
            {
                data.entries.Clear();
            }
            GUI.backgroundColor = defaultGUIBackgrounColor;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10.0f);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    void StartEntry(Entry entry)
    {
        int standaloneCount = entry.count;
        if (!Application.isPlaying && entry.runInEditor)
        {
            //EditorLevelManager.StartGameInEditor(args);
            standaloneCount--;
        }

        for (var i = 0; i < standaloneCount; i++)
        {
            BuildUtils.RunBuild(entry.name);
        }
    }

    void DrawLine()
    {
        EditorGUILayout.Space();
        Rect rect = EditorGUILayout.BeginHorizontal();
        Handles.color = Color.gray;
        Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }
}