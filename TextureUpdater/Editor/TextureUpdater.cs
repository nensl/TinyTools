// Copyright (C) 2023 Nenad Slavujevic All Rights Reserved

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TextureUpdater : EditorWindow
{
    // Setup
    public DefaultAsset[] directories = { };
    public Texture[] textures = { };
    private SerializedObject _serializedTarget;
    private FileSelectionMode _fileSelectionMode = FileSelectionMode.All;
    private BuildTarget _currentBuildTarget = BuildTarget.StandaloneWindows;

    // Settings
    private TextureImporterPlatformSettings _textureImporterSettings;
    private string[] _resolutions = {"32", "64", "128", "256", "512", "1024", "2048", "4096", "8192"};
    private int _formatPopupIndex;
    private int _maxSizePopupIndex;
    private int _limitSizePopupIndex;
    private bool _skipOverride;
    private bool _overrideDefaultMaxSize;
    private bool _limitMaxSize;
    private bool _initialized;

    // Progress
    private int _allItems;
    private int _processedItems;
    private bool _processStarted;

    // GUI
    private GUIStyle _bigBoldLabelStyle;
    private GUIStyle _mediumBoldLabelStyle;
    private GUIStyle _smallSubtitleStyle;

    [MenuItem("Tiny Tools/Texture Updater")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow<TextureUpdater>("Texture Updater");
        window.maxSize = new Vector2(400f, 500f);
        window.minSize = window.maxSize;
    }

    private void OnEnable()
    {
        ScriptableObject target = this;
        _serializedTarget = new SerializedObject(target);
        _textureImporterSettings = new TextureImporterPlatformSettings();
    }

    private void OnGUI()
    {
        if (!_initialized)
            Init();

        EditorGUILayout.Space();
        RenderHeader();
        EditorGUILayout.Space();
        RenderList();
        EditorGUILayout.Space();
        RenderSelectionMode();
        EditorGUILayout.Space(15);
        RenderPlatformOptions();
        EditorGUILayout.Space(15);
        RenderAdditionalOptions();
        EditorGUILayout.Space(15);
        RenderConfirm();
    }

    private void Init()
    {
        InitStyles();

        _initialized = true;
    }

    private void InitStyles()
    {
        _bigBoldLabelStyle = new GUIStyle
        {
            fontSize = 16,
            alignment = TextAnchor.UpperCenter,
            fontStyle = FontStyle.Bold,
            normal = {textColor = new Color(0.29f, 0.76f, 0.93f)}
        };

        _mediumBoldLabelStyle = new GUIStyle
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = {textColor = new Color(0.73f, 0.73f, 0.73f)}
        };

        _smallSubtitleStyle = new GUIStyle
        {
            fontSize = 8,
            alignment = TextAnchor.UpperCenter,
            fontStyle = FontStyle.Bold,
            normal = {textColor = new Color(0.29f, 0.65f, 0.93f)}
        };
    }

    private void RenderHeader()
    {
        EditorGUILayout.LabelField("Texture Updater", _bigBoldLabelStyle);
    }

    private void RenderList()
    {
        EditorGUILayout.LabelField($"{_fileSelectionMode.ToString()} Mode", _mediumBoldLabelStyle);

        if (_fileSelectionMode == FileSelectionMode.All)
        {
            EditorGUILayout.HelpBox(
                "You're in All Mode. Make sure that's what you want.",
                MessageType.Warning,
                true);
            return;
        }

        _serializedTarget.Update();
        SerializedProperty property = null;
        switch (_fileSelectionMode)
        {
            case FileSelectionMode.Directory:
                property = _serializedTarget.FindProperty("directories");
                break;
            case FileSelectionMode.Texture:
                property = _serializedTarget.FindProperty("textures");
                break;
        }

        EditorGUILayout.PropertyField(property, true);

        if (_fileSelectionMode == FileSelectionMode.Directory)
        {
            var nonDirectoryDetected = false;
            foreach (var directory in directories)
                if (!AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(directory)))
                {
                    nonDirectoryDetected = true;
                    break;
                }

            if (nonDirectoryDetected)
                EditorGUILayout.HelpBox(
                    "Make sure to only add folders!",
                    MessageType.Info,
                    true);
        }

        _serializedTarget.ApplyModifiedProperties();
    }

    private bool IsListReady()
    {
        return _fileSelectionMode == FileSelectionMode.Directory && directories.Length > 0 &&
               directories.Count(x => x == null) == 0
               || _fileSelectionMode == FileSelectionMode.Texture && textures.Length > 0 &&
               textures.Count(x => x == null) == 0;
    }

    private void RenderSelectionMode()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();

        foreach (var mode in Enum.GetValues(typeof(FileSelectionMode)).Cast<FileSelectionMode>())
        {
            if (mode == _fileSelectionMode)
                continue;

            if (GUILayout.Button($"{mode.ToString()} Mode", GUILayout.Width(120)))
                _fileSelectionMode = mode;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void RenderPlatformOptions()
    {
        UpdatePlatform();
        UpdateTextureOptions();
    }

    private void UpdatePlatform()
    {
        EditorGUI.BeginChangeCheck();
        _currentBuildTarget = (BuildTarget) EditorGUILayout.EnumPopup("Build Target", _currentBuildTarget);
        if (EditorGUI.EndChangeCheck())
            _formatPopupIndex = 0;
    }

    private void UpdateTextureOptions()
    {
        var enumList = Enum.GetValues(typeof(TextureImporterFormat)).Cast<TextureImporterFormat>()
            .OrderBy(x => x.ToString())
            .Where(x => TextureImporter.IsPlatformTextureFormatValid(TextureImporterType.Sprite,
                _currentBuildTarget, x));

        var enumDictionary = new Dictionary<int, string>();

        foreach (var element in enumList)
            if (!enumDictionary.ContainsKey((int) element))
                enumDictionary.Add((int) element, element.ToString());

        _textureImporterSettings.overridden = true;

        _formatPopupIndex =
            EditorGUILayout.Popup("Texture Format", _formatPopupIndex, enumDictionary.Values.ToArray());
        _textureImporterSettings.format =
            (TextureImporterFormat) Enum.Parse(typeof(TextureImporterFormat), enumDictionary.Values
                .ToArray()[_formatPopupIndex]);

        _overrideDefaultMaxSize = EditorGUILayout.Toggle("Override Default Max Size", _overrideDefaultMaxSize);
        EditorGUI.BeginDisabledGroup(!_overrideDefaultMaxSize);
        _maxSizePopupIndex = EditorGUILayout.Popup("Max Size", _maxSizePopupIndex, _resolutions);
        _textureImporterSettings.maxTextureSize = int.Parse(_resolutions[_maxSizePopupIndex]);
        EditorGUI.EndDisabledGroup();
        
        _limitMaxSize = EditorGUILayout.Toggle("Limit Max Size", _limitMaxSize);
        EditorGUI.BeginDisabledGroup(!_limitMaxSize);
        _limitSizePopupIndex = EditorGUILayout.Popup("Max Size", _limitSizePopupIndex, _resolutions);
        _textureImporterSettings.maxTextureSize = int.Parse(_resolutions[_limitSizePopupIndex]);
        EditorGUI.EndDisabledGroup();

        _textureImporterSettings.crunchedCompression = EditorGUILayout.Toggle("Crunch Compression",
            _textureImporterSettings.crunchedCompression);

        EditorGUI.BeginDisabledGroup(!_textureImporterSettings.crunchedCompression);
        _textureImporterSettings.compressionQuality = EditorGUILayout.IntSlider("Compression Quality",
            _textureImporterSettings.compressionQuality, 0, 100);
        EditorGUI.EndDisabledGroup();
    }

    private void RenderAdditionalOptions()
    {
        _skipOverride = EditorGUILayout.Toggle("Skip Already Overridden", _skipOverride);
    }

    private void RenderConfirm()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.green;
        EditorGUI.BeginDisabledGroup(!IsListReady() || _processStarted);
        if (GUILayout.Button("Go", GUILayout.Width(85))) UpdateTextures();
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = oldColor;
        GUILayout.Space(4);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void UpdateTextures()
    {
        _allItems = 0;
        _processedItems = 0;

        var platformString = BuildPlatformToPlatformString(_currentBuildTarget);

        if (string.IsNullOrEmpty(platformString))
        {
            Debug.LogError("Platform not yet supported.");
            return;
        }

        string[] guidsToChange;

        if (_fileSelectionMode == FileSelectionMode.All)
        {
            guidsToChange = AssetDatabase.FindAssets("t:Texture", new[] {"Assets"});
        }
        else if (_fileSelectionMode == FileSelectionMode.Directory)
        {
            var tempPaths = new List<string>();
            foreach (var directory in directories)
                if (!tempPaths.Contains(AssetDatabase.GetAssetPath(directory)))
                    tempPaths.Add(AssetDatabase.GetAssetPath(directory));

            var searchInFolders = tempPaths.ToArray();

            guidsToChange = AssetDatabase.FindAssets("t:Texture", searchInFolders);
        }
        else if (_fileSelectionMode == FileSelectionMode.Texture)
        {
            var tempGuids = new List<string>();

            foreach (var texture in textures)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture, out var guid, out long _))
                    tempGuids.Add(guid);

            guidsToChange = tempGuids.ToArray();
        }
        else
        {
            Debug.LogError("Something went wrong!");
            return;
        }

        _allItems = guidsToChange.Length;

        _processStarted = true;

        foreach (var guid in guidsToChange)
        {
            var progress = (float) _processedItems / _allItems;

            if (EditorUtility.DisplayCancelableProgressBar("Cancelable",
                $"{_processedItems}/{_allItems} Items Processed", progress))
            {
                Reset();
                break;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null) continue;

            var currentPlatformTextureSettings = importer.GetPlatformTextureSettings(platformString);
            var changed = false;

            ChangeIfNecessary(currentPlatformTextureSettings);

            if (changed)
            {
                importer.SaveAndReimport();
                _processedItems++;
            }

            void ChangeIfNecessary(TextureImporterPlatformSettings platSettings)
            {
                if (_skipOverride && platSettings.overridden)
                    return;

                if (platSettings.format == _textureImporterSettings.format
                    && platSettings.maxTextureSize == _textureImporterSettings.maxTextureSize
                    && platSettings.crunchedCompression == _textureImporterSettings.crunchedCompression
                    && platSettings.compressionQuality == _textureImporterSettings.compressionQuality)
                    return;

                platSettings.overridden = true;
                platSettings.format = _textureImporterSettings.format;
                if(_overrideDefaultMaxSize)
                    platSettings.maxTextureSize = _textureImporterSettings.maxTextureSize;
                if (_limitMaxSize && platSettings.maxTextureSize > int.Parse(_resolutions[_limitSizePopupIndex]))
                    platSettings.maxTextureSize = int.Parse(_resolutions[_limitSizePopupIndex]);
                platSettings.crunchedCompression = _textureImporterSettings.crunchedCompression;
                platSettings.compressionQuality = _textureImporterSettings.compressionQuality;

                changed = true;
                importer.SetPlatformTextureSettings(platSettings);
            }
        }

        Reset();
    }

    private void Reset()
    {
        _processStarted = false;
        EditorUtility.ClearProgressBar();
    }

    private string BuildPlatformToPlatformString(BuildTarget currentBuildTarget)
    {
        switch (currentBuildTarget)
        {
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneOSX:
                return "Standalone";
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iPhone";
            case BuildTarget.Switch:
                return "Switch";
            case BuildTarget.GameCoreXboxOne:
                return "GameCoreXboxOne";
            case BuildTarget.GameCoreXboxSeries:
                return "GameCoreXboxSeries";
            case BuildTarget.PS4:
                return "PS4";
            case BuildTarget.PS5:
                return "PS5";
            case BuildTarget.tvOS:
                return "tvOS";
            case BuildTarget.WebGL:
                return "WebGL";
            default:
                return string.Empty;
        }
    }
}

public enum FileSelectionMode
{
    All,
    Directory,
    Texture
}