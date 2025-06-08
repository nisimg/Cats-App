using UnityEngine;
using UnityEditor;
using System.IO;

public class ProjectStructureCreator : EditorWindow
{
    private string projectName = "";
    private bool createScenes = true;
    private bool createPrefabs = true;
    private bool createMaterials = true;
    private bool createAudio = true;
    private bool createUI = true;
    private bool createData = true;
    private bool createResources = true;
    private bool createEditorScripts = true;

    // Standard Unity project folders
    private readonly string[] standardFolders = {
        "Scripts",
        "Scenes", 
        "Prefabs",
        "Materials",
        "Textures",
        "Audio",
        "Animations",
        "Fonts",
        "Models",
        "Shaders",
        "UI",
        "Data",
        "Resources",
        "StreamingAssets",
        "Plugins",
        "Editor"
    };

    // Sub-folders for Scripts
    private readonly string[] scriptSubFolders = {
        "Scripts/Managers",
        "Scripts/Player",
        "Scripts/UI",
        "Scripts/Utilities",
        "Scripts/GameLogic",
        "Scripts/Audio",
        "Scripts/Data",
        "Scripts/AI"
    };

    // Sub-folders for Audio
    private readonly string[] audioSubFolders = {
        "Audio/Music",
        "Audio/SFX",
        "Audio/Voice",
        "Audio/Ambience"
    };

    // Sub-folders for Textures
    private readonly string[] textureSubFolders = {
        "Textures/UI",
        "Textures/Characters",
        "Textures/Environment",
        "Textures/Effects",
        "Textures/Icons"
    };

    // Sub-folders for UI
    private readonly string[] uiSubFolders = {
        "UI/Sprites",
        "UI/Prefabs",
        "UI/Layouts"
    };

    // Sub-folders for Data
    private readonly string[] dataSubFolders = {
        "Data/ScriptableObjects",
        "Data/Configs",
        "Data/SaveData"
    };

    // Editor sub-folders
    private readonly string[] editorSubFolders = {
        "Editor/Tools",
        "Editor/CustomInspectors",
        "Editor/BuildScripts"
    };

    [MenuItem("Tools/NGS/Create Project Structure")]
    public static void ShowWindow()
    {
        ProjectStructureCreator window = GetWindow<ProjectStructureCreator>();
        window.titleContent = new GUIContent("Project Structure Creator");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Unity Project Structure Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Project name input
        EditorGUILayout.LabelField("Project Configuration", EditorStyles.boldLabel);
        projectName = EditorGUILayout.TextField("Project Name:", projectName);
        
        if (string.IsNullOrEmpty(projectName))
        {
            EditorGUILayout.HelpBox("Please enter a project name to create the folder structure.", MessageType.Warning);
        }

        GUILayout.Space(10);

        // Folder options
        EditorGUILayout.LabelField("Folder Options", EditorStyles.boldLabel);
        createScenes = EditorGUILayout.Toggle("Create Scene Folders", createScenes);
        createPrefabs = EditorGUILayout.Toggle("Create Prefab Folders", createPrefabs);
        createMaterials = EditorGUILayout.Toggle("Create Material Folders", createMaterials);
        createAudio = EditorGUILayout.Toggle("Create Audio Folders", createAudio);
        createUI = EditorGUILayout.Toggle("Create UI Folders", createUI);
        createData = EditorGUILayout.Toggle("Create Data Folders", createData);
        createResources = EditorGUILayout.Toggle("Create Resources Folder", createResources);
        createEditorScripts = EditorGUILayout.Toggle("Create Editor Script Folders", createEditorScripts);

        GUILayout.Space(20);

        // Preview
        EditorGUILayout.LabelField("Folder Structure Preview:", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(GetFolderPreview(), GUILayout.Height(200));

        GUILayout.Space(10);

        // Create button
        GUI.enabled = !string.IsNullOrEmpty(projectName);
        if (GUILayout.Button("Create Project Structure", GUILayout.Height(30)))
        {
            CreateProjectStructure();
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        // Info
        EditorGUILayout.HelpBox(
            "This will create a folder structure inside '_" + projectName + "' folder in your Assets directory.\n" +
            "All project assets will be organized within this main folder.",
            MessageType.Info);
    }

    private string GetFolderPreview()
    {
        if (string.IsNullOrEmpty(projectName))
            return "Enter a project name to see preview...";

        string preview = $"Assets/_{projectName}/\n";
        
        // Add main folders
        foreach (string folder in standardFolders)
        {
            if (ShouldCreateFolder(folder))
            {
                preview += $"├── {folder}/\n";
                
                // Add sub-folders based on the main folder
                string[] subFolders = GetSubFoldersForMainFolder(folder);
                if (subFolders != null)
                {
                    for (int i = 0; i < subFolders.Length; i++)
                    {
                        string subFolder = subFolders[i].Substring(folder.Length + 1); // Remove main folder part
                        string prefix = i == subFolders.Length - 1 ? "└──" : "├──";
                        preview += $"│   {prefix} {subFolder}/\n";
                    }
                }
            }
        }

        return preview;
    }

    private string[] GetSubFoldersForMainFolder(string mainFolder)
    {
        switch (mainFolder)
        {
            case "Scripts":
                return scriptSubFolders;
            case "Audio":
                return createAudio ? audioSubFolders : null;
            case "Textures":
                return textureSubFolders;
            case "UI":
                return createUI ? uiSubFolders : null;
            case "Data":
                return createData ? dataSubFolders : null;
            case "Editor":
                return createEditorScripts ? editorSubFolders : null;
            default:
                return null;
        }
    }

    private bool ShouldCreateFolder(string folder)
    {
        switch (folder)
        {
            case "Scenes":
                return createScenes;
            case "Prefabs":
                return createPrefabs;
            case "Materials":
                return createMaterials;
            case "Audio":
                return createAudio;
            case "UI":
                return createUI;
            case "Data":
                return createData;
            case "Resources":
                return createResources;
            case "Editor":
                return createEditorScripts;
            default:
                return true; // Create all other folders by default
        }
    }

    private void CreateProjectStructure()
    {
        string mainFolderPath = "Assets/_" + projectName;
        
        // Create main project folder
        if (!AssetDatabase.IsValidFolder(mainFolderPath))
        {
            AssetDatabase.CreateFolder("Assets", "_" + projectName);
            Debug.Log($"Created main project folder: {mainFolderPath}");
        }

        // Create standard folders
        foreach (string folder in standardFolders)
        {
            if (ShouldCreateFolder(folder))
            {
                string folderPath = mainFolderPath + "/" + folder;
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder(mainFolderPath, folder);
                    Debug.Log($"Created folder: {folderPath}");
                }
            }
        }

        // Create sub-folders
        CreateSubFolders(mainFolderPath, scriptSubFolders);
        
        if (createAudio)
            CreateSubFolders(mainFolderPath, audioSubFolders);
        
        CreateSubFolders(mainFolderPath, textureSubFolders);
        
        if (createUI)
            CreateSubFolders(mainFolderPath, uiSubFolders);
        
        if (createData)
            CreateSubFolders(mainFolderPath, dataSubFolders);
        
        if (createEditorScripts)
            CreateSubFolders(mainFolderPath, editorSubFolders);

        // Refresh the asset database
        AssetDatabase.Refresh();
        
        // Show success message
        EditorUtility.DisplayDialog("Success", 
            $"Project structure for '{projectName}' has been created successfully!\n\n" +
            $"All folders are organized under: {mainFolderPath}", "OK");

        Debug.Log($"Project structure creation completed for '{projectName}'");
    }

    private void CreateSubFolders(string mainFolderPath, string[] subFolders)
    {
        foreach (string subFolder in subFolders)
        {
            string fullPath = mainFolderPath + "/" + subFolder;
            string[] pathParts = subFolder.Split('/');
            string currentPath = mainFolderPath;
            
            // Create nested folders one by one
            for (int i = 0; i < pathParts.Length; i++)
            {
                string nextPath = currentPath + "/" + pathParts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                    Debug.Log($"Created sub-folder: {nextPath}");
                }
                currentPath = nextPath;
            }
        }
    }

    // Additional utility method to create a README file in the main folder
    [MenuItem("Tools/Create Project Structure/Add README")]
    public static void CreateReadmeFile()
    {
        string projectName = PlayerSettings.productName;
        string mainFolderPath = "Assets/_" + projectName;
        
        if (!AssetDatabase.IsValidFolder(mainFolderPath))
        {
            EditorUtility.DisplayDialog("Error", 
                $"Project folder '{mainFolderPath}' not found. Please create the project structure first.", "OK");
            return;
        }

        string readmePath = mainFolderPath + "/README.txt";
        string readmeContent = $@"Project: {projectName}
Created: {System.DateTime.Now}

Folder Structure:
================
Scripts/        - All C# scripts organized by category
Scenes/         - Unity scene files
Prefabs/        - Prefab assets
Materials/      - Material assets
Textures/       - Texture and sprite assets
Audio/          - Sound effects, music, and audio clips
Animations/     - Animation clips and controllers
Fonts/          - Font assets
Models/         - 3D models and meshes
Shaders/        - Custom shader files
UI/             - UI assets and prefabs
Data/           - ScriptableObjects and data files
Resources/      - Assets loaded at runtime
StreamingAssets/- Assets included in builds
Plugins/        - Third-party plugins and libraries
Editor/         - Editor-only scripts and tools

Guidelines:
===========
- Keep all project assets within this folder
- Use consistent naming conventions
- Document important scripts and systems
- Organize assets logically within subfolders
";

        File.WriteAllText(readmePath, readmeContent);
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", $"README.txt created in {mainFolderPath}", "OK");
    }
}