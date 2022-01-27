// Based on Code by Exit Games / Photon Quantum

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class ToolbarUtilities
{
    private static ScriptableObject _toolbar;
    private static string[]         _scenePaths;
    private static string[]         _sceneNames;
  
    static ToolbarUtilities() {
        EditorApplication.delayCall += () => {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        };
    }

    private static void Update()
    {
        if (_toolbar == null)
        {
            var editorAssembly = typeof(Editor).Assembly;

            var toolbars = Resources.FindObjectsOfTypeAll(editorAssembly.GetType("UnityEditor.Toolbar"));
            _toolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
            if (_toolbar != null)
            {
                var windowBackendPropertyInfo = editorAssembly.GetType("UnityEditor.GUIView").GetProperty("windowBackend",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (windowBackendPropertyInfo is { })
                {
                    var windowBackend = windowBackendPropertyInfo.GetValue(_toolbar);
                    var visualTreePropertyInfo = windowBackend.GetType()
                        .GetProperty("visualTree", BindingFlags.Public | BindingFlags.Instance);
                    if (visualTreePropertyInfo is { })
                    {
                        var visualTree = (VisualElement) visualTreePropertyInfo.GetValue(windowBackend);

                        var container = (IMGUIContainer) visualTree[0];

                        var onGUIHandlerFieldInfo = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (onGUIHandlerFieldInfo is { })
                        {
                            var handler = (Action) onGUIHandlerFieldInfo.GetValue(container);
                            handler -= OnGUI;
                            handler += OnGUI;
                            onGUIHandlerFieldInfo.SetValue(container, handler);
                        }
                    }
                }
            }
        }
        
        if (_scenePaths == null || _scenePaths.Length != EditorBuildSettings.scenes.Length)
        {
            List<string> scenePaths = new List<string>();
            List<string> sceneNames = new List<string>();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
              if (scene.path == null || scene.path.StartsWith("Assets") == false)
                continue;

              string scenePath = Application.dataPath + scene.path.Substring(6);

              scenePaths.Add(scenePath);
              sceneNames.Add(Path.GetFileNameWithoutExtension(scenePath));
            }

            _scenePaths = scenePaths.ToArray();
            _sceneNames = sceneNames.ToArray();
        }
    }
    
    private static void OnGUI() {

        using (new EditorGUI.DisabledScope(Application.isPlaying)) {
            var rect = new Rect(0, 0, Screen.width, Screen.height)
            {
                xMin = EditorGUIUtility.currentViewWidth * 0.5f + 100.0f,
                xMax = EditorGUIUtility.currentViewWidth - 350.0f,
                y = 8.0f
            };

            using (new GUILayout.AreaScope(rect)) {
                var sceneName  = SceneManager.GetActiveScene().name;
                var sceneIndex = -1;

                for (var i = 0; i < _sceneNames.Length; ++i)
                {
                    if (sceneName != _sceneNames[i]) 
                        continue;
                    sceneIndex = i;
                    break;
                }

                var newSceneIndex = EditorGUILayout.Popup(sceneIndex, _sceneNames, GUILayout.Width(200.0f));
                if (newSceneIndex == sceneIndex) 
                    return;
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                    EditorSceneManager.OpenScene(_scenePaths[newSceneIndex], OpenSceneMode.Single);
                }
            }
        }
    }
}

    