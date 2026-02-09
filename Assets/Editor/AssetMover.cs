using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VitsehLand.Editor.Tools.Assets
{
    public class AssetMover : EditorWindow
    {
        // File type filter flags
        [System.Flags]
        private enum FileType
        {
            None = 0,
            CS = 1 << 0,
            Animation = 1 << 1,
            Animator = 1 << 2,
            Prefab = 1 << 3,
            Material = 1 << 4,
            Texture = 1 << 5,
            Audio = 1 << 6,
            Model = 1 << 7,
            ScriptableObject = 1 << 8,
            All = ~0
        }

        // Class to hold file information
        [System.Serializable]
        private class AssetItem
        {
            public string path;
            public string fileName;
            public bool isUsed;
            public Object asset;
            public FileType type;
            public List<string> dependencies;
            public List<string> referencedBy;
            public bool isIndirectlyUsed;

            private bool usageChecked = false;
            private bool dependenciesChecked = false;

            private List<string> scanFolders;
            private bool scanAll;
            private bool deepScan;

            public AssetItem(string assetPath, List<string> foldersToScan = null, bool scanAllFolders = true, bool thoroughScan = false)
            {
                this.path = assetPath;
                this.fileName = Path.GetFileName(assetPath);
                this.asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                this.type = DetermineFileType(assetPath);
                this.dependencies = new List<string>();
                this.referencedBy = new List<string>();
                this.scanFolders = foldersToScan ?? new List<string>();
                this.scanAll = scanAllFolders;
                this.deepScan = thoroughScan;
                this.isIndirectlyUsed = false;
            }

            public bool GetIsUsed()
            {
                if (usageCache.ContainsKey(path))
                {
                    isUsed = usageCache[path];
                    usageChecked = true;
                    return isUsed;
                }

                if (!usageChecked)
                {
                    isUsed = CheckIfUsed(path);
                    usageChecked = true;
                    usageCache[path] = isUsed;
                }
                return isUsed;
            }

            public void EnsureDependenciesLoaded()
            {
                if (!dependenciesChecked)
                {
                    FindDependencies(path);
                    dependenciesChecked = true;
                }
            }

            public void ResetCache()
            {
                usageChecked = false;
                dependenciesChecked = false;
                isIndirectlyUsed = false;
            }

            private FileType DetermineFileType(string path)
            {
                string ext = Path.GetExtension(path).ToLower();
                switch (ext)
                {
                    case ".cs": return FileType.CS;
                    case ".anim": return FileType.Animation;
                    case ".controller": return FileType.Animator;
                    case ".prefab": return FileType.Prefab;
                    case ".mat": return FileType.Material;
                    case ".asset": return FileType.ScriptableObject;
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".tga":
                    case ".psd": return FileType.Texture;
                    case ".mp3":
                    case ".wav":
                    case ".ogg": return FileType.Audio;
                    case ".fbx":
                    case ".obj": return FileType.Model;
                    default: return FileType.None;
                }
            }

            private bool CheckIfUsed(string assetPath)
            {
                if (scanAll)
                {
                    return CheckIfUsedInProject(assetPath);
                }

                if (scanFolders == null || scanFolders.Count == 0)
                {
                    return false;
                }

                return CheckIfUsedInFolders(assetPath, scanFolders);
            }

            private bool CheckIfUsedInProject(string assetPath)
            {
                string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

                foreach (string asset in allAssetPaths)
                {
                    if (asset == assetPath) continue;

                    bool shouldCheck = false;

                    if (deepScan)
                    {
                        shouldCheck = !asset.EndsWith(".cs") && !asset.EndsWith(".meta") && !asset.EndsWith(".dll");
                    }
                    else
                    {
                        shouldCheck = asset.EndsWith(".unity") || asset.EndsWith(".prefab") || asset.EndsWith(".asset");
                    }

                    if (shouldCheck)
                    {
                        string[] deps;
                        if (dependencyCache.ContainsKey(asset))
                        {
                            deps = dependencyCache[asset];
                        }
                        else
                        {
                            deps = AssetDatabase.GetDependencies(asset, true);
                            dependencyCache[asset] = deps;
                        }

                        if (deps.Contains(assetPath))
                            return true;
                    }
                }

                return false;
            }

            private bool CheckIfUsedInFolders(string assetPath, List<string> foldersToScan)
            {
                foreach (string folder in foldersToScan)
                {
                    List<string> assetsToCheck;

                    if (scanFolderCache.ContainsKey(folder))
                    {
                        assetsToCheck = scanFolderCache[folder];
                    }
                    else
                    {
                        assetsToCheck = BuildFolderCache(folder, deepScan);
                        scanFolderCache[folder] = assetsToCheck;
                    }

                    foreach (string asset in assetsToCheck)
                    {
                        if (asset == assetPath) continue;

                        string[] deps;
                        if (dependencyCache.ContainsKey(asset))
                        {
                            deps = dependencyCache[asset];
                        }
                        else
                        {
                            deps = AssetDatabase.GetDependencies(asset, true);
                            dependencyCache[asset] = deps;
                        }

                        if (deps.Contains(assetPath))
                            return true;
                    }
                }

                return false;
            }

            private void FindDependencies(string assetPath)
            {
                string[] deps;

                if (dependencyCache.ContainsKey(assetPath))
                {
                    deps = dependencyCache[assetPath];
                }
                else
                {
                    deps = AssetDatabase.GetDependencies(assetPath, false);
                    dependencyCache[assetPath] = deps;
                }

                foreach (string dep in deps)
                {
                    if (dep != assetPath && !dep.EndsWith(".cs"))
                    {
                        dependencies.Add(dep);
                    }
                }

                List<string> assetsToCheck = new List<string>();

                if (scanAll)
                {
                    assetsToCheck.AddRange(AssetDatabase.GetAllAssetPaths());
                }
                else if (scanFolders != null && scanFolders.Count > 0)
                {
                    foreach (string folder in scanFolders)
                    {
                        if (scanFolderCache.ContainsKey(folder))
                        {
                            assetsToCheck.AddRange(scanFolderCache[folder]);
                        }
                        else
                        {
                            List<string> folderAssets = BuildFolderCache(folder, deepScan);
                            scanFolderCache[folder] = folderAssets;
                            assetsToCheck.AddRange(folderAssets);
                        }
                    }
                }
                else
                {
                    return;
                }

                foreach (string asset in assetsToCheck)
                {
                    if (asset == assetPath) continue;

                    string[] assetDeps;
                    if (dependencyCache.ContainsKey(asset))
                    {
                        assetDeps = dependencyCache[asset];
                    }
                    else
                    {
                        assetDeps = AssetDatabase.GetDependencies(asset, false);
                        dependencyCache[asset] = assetDeps;
                    }

                    if (assetDeps.Contains(assetPath))
                    {
                        referencedBy.Add(asset);
                    }
                }
            }
        }

        // Editor variables
        private List<AssetItem> fileList = new List<AssetItem>();
        private List<AssetItem> usedFileList = new List<AssetItem>();

        private Vector2 GUIScrollPosition, scrollPosition;
        private Vector2 scanFoldersScrollPosition;
        private string destinationPath = "Assets/";
        private string excludePath = "Assets/"; // Exclude this path prefix
        private FileType selectedFileTypes = FileType.All;
        private bool autoExtractOnDrop = true;
        private bool showUsedOnly = false;
        private bool showUnusedOnly = false;
        private bool moveDependencies = true;
        private bool preserveDependencyFolders = true;
        private bool showDependencies = false;
        private AssetItem selectedItemForDetails = null;

        // Auto-tag
        private bool autoTagDependentAssets = false;
        private bool isProcessingAutoTag = false;
        private int autoTagProcessedCount = 0;

        // Scan scope
        private List<string> scanFolders = new List<string>();
        private bool scanAllFolders = false;
        private bool showScanSettings = true;
        private bool thoroughScan = false;

        // Batch processing
        private bool isProcessingUsage = false;
        private int processingIndex = 0;
        private List<AssetItem> itemsToProcess = new List<AssetItem>();

        // Static caches
        private static Dictionary<string, string[]> dependencyCache = new Dictionary<string, string[]>();
        private static Dictionary<string, bool> usageCache = new Dictionary<string, bool>();
        private static Dictionary<string, List<string>> scanFolderCache = new Dictionary<string, List<string>>();

        // Window size
        private float dragDropHeight = 150f;
        private float detailedListHeight = 200f;
        private float scanFoldersHeight = 150f;
        private const float MIN_HEIGHT = 50f;
        private const float MAX_HEIGHT = 500f;

        [MenuItem("Tools/Asset Mover")]
        public static void ShowWindow()
        {
            GetWindow<AssetMover>("Asset Mover");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            isProcessingUsage = false;
            isProcessingAutoTag = false;
        }

        private void OnEditorUpdate()
        {
            if (isProcessingUsage && itemsToProcess != null && itemsToProcess.Count > 0)
            {
                ProcessNextBatch();
            }
        }

        private void StartBatchUsageCheck()
        {
            if (!scanAllFolders && (scanFolders == null || scanFolders.Count == 0))
            {
                EditorUtility.DisplayDialog("No Scan Scope", "Add folders or enable full scan.", "OK");
                return;
            }

            if (scanAllFolders)
            {
                if (!EditorUtility.DisplayDialog("Scan All?", "May be slow. Continue?", "Yes", "Cancel"))
                {
                    return;
                }
            }

            itemsToProcess = new List<AssetItem>(fileList);
            processingIndex = 0;
            isProcessingUsage = true;
        }

        private void ProcessNextBatch()
        {
            int batchSize = 5;
            int processed = 0;

            while (processed < batchSize && processingIndex < itemsToProcess.Count)
            {
                itemsToProcess[processingIndex].GetIsUsed();
                processingIndex++;
                processed++;
            }

            if (processingIndex >= itemsToProcess.Count)
            {
                isProcessingUsage = false;

                if (autoTagDependentAssets)
                {
                    ProcessAutoTagDependentAssets();
                }

                Repaint();
            }
            else
            {
                Repaint();
            }
        }

        private void ProcessAutoTagDependentAssets()
        {
            isProcessingAutoTag = true;
            autoTagProcessedCount = 0;

            HashSet<string> usedAssetPaths = new HashSet<string>();
            foreach (var item in fileList)
            {
                if (item.GetIsUsed())
                {
                    usedAssetPaths.Add(item.path);
                }
            }

            int taggedCount = 0;
            foreach (var item in fileList)
            {
                if (!item.GetIsUsed())
                {
                    item.EnsureDependenciesLoaded();

                    bool hasUsedDependency = false;
                    foreach (string dep in item.dependencies)
                    {
                        if (usedAssetPaths.Contains(dep))
                        {
                            hasUsedDependency = true;
                            break;
                        }
                    }

                    if (hasUsedDependency && item.path.EndsWith(".prefab"))
                    {
                        item.isIndirectlyUsed = true;
                        item.isUsed = true;
                        usageCache[item.path] = true;
                        taggedCount++;
                        autoTagProcessedCount++;
                    }
                }
            }

            isProcessingAutoTag = false;

            if (taggedCount > 0)
            {
                EditorUtility.DisplayDialog("Auto-Tag", $"Tagged {taggedCount} assets", "OK");
            }

            Repaint();
        }

        private void ManualAutoTag()
        {
            if (!scanAllFolders && (scanFolders == null || scanFolders.Count == 0))
            {
                EditorUtility.DisplayDialog("No Scope", "Add folders first.", "OK");
                return;
            }

            if (fileList.Count == 0)
            {
                EditorUtility.DisplayDialog("No Files", "No files to process.", "OK");
                return;
            }

            bool hasCheckedUsage = fileList.Any(item => usageCache.ContainsKey(item.path));
            if (!hasCheckedUsage)
            {
                bool proceed = EditorUtility.DisplayDialog("Not Checked", "Check usage first?", "Yes", "Cancel");

                if (proceed)
                {
                    StartBatchUsageCheck();
                }
                return;
            }

            ProcessAutoTagDependentAssets();
        }

        private void ClearCache()
        {
            dependencyCache.Clear();
            usageCache.Clear();
            scanFolderCache.Clear();

            foreach (var item in fileList)
            {
                item.ResetCache();
            }

            EditorUtility.DisplayDialog("Cleared", "All caches cleared.", "OK");
        }

        private float DrawResizeSlider(float currentHeight, string label)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150));
            float newHeight = EditorGUILayout.Slider(currentHeight, MIN_HEIGHT, MAX_HEIGHT);
            EditorGUILayout.EndHorizontal();
            return newHeight;
        }

        private void OnGUI()
        {
            GUILayout.Label("Asset Mover", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUIScrollPosition = EditorGUILayout.BeginScrollView(GUIScrollPosition, GUILayout.Height(Screen.height * 0.7f));

            if (!scanAllFolders && (scanFolders == null || scanFolders.Count == 0))
            {
                EditorGUILayout.HelpBox("NO SCAN SCOPE!", MessageType.Error);
            }

            showScanSettings = EditorGUILayout.Foldout(showScanSettings, "Scan Settings", true);
            if (showScanSettings)
            {
                EditorGUILayout.BeginVertical("box");
                DrawScanSettings();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            DrawFileTypeFilter();
            EditorGUILayout.Space();

            autoExtractOnDrop = EditorGUILayout.Toggle("Auto Extract", autoExtractOnDrop);
            if (autoExtractOnDrop)
            {
                EditorGUILayout.HelpBox("Only files matching selected types will be added", MessageType.None);
            }

            moveDependencies = EditorGUILayout.Toggle("Move Dependencies", moveDependencies);
            if (moveDependencies)
            {
                EditorGUILayout.HelpBox("Materials, textures, models will move with main files", MessageType.None);
            }

            GUI.enabled = moveDependencies;
            preserveDependencyFolders = EditorGUILayout.Toggle("  Preserve Folders", preserveDependencyFolders);
            GUI.enabled = true;

            // Exclude Path (only when preserve enabled)
            if (preserveDependencyFolders)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Exclude Prefix:", GUILayout.Width(100));
                excludePath = EditorGUILayout.TextField(excludePath);

                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string selectedFolder = EditorUtility.OpenFolderPanel("Select Exclude Path", "Assets", "");
                    if (!string.IsNullOrEmpty(selectedFolder))
                    {
                        if (selectedFolder.StartsWith(Application.dataPath))
                        {
                            excludePath = "Assets" + selectedFolder.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder within the Unity project.", "OK");
                        }
                    }
                }

                if (GUILayout.Button("?", GUILayout.Width(20)))
                {
                    EditorUtility.DisplayDialog("Exclude Path",
                        "Remove this prefix from paths.\n\n" +
                        "Example:\n" +
                        "File: Assets/A/B/C/file.fbx\n" +
                        "Exclude: Assets/A\n" +
                        "Result: {dest}/B/C/file.fbx",
                        "OK");
                }
                EditorGUILayout.EndHorizontal();

                string displayExclude = string.IsNullOrEmpty(excludePath) ? "Assets/" : excludePath;
                EditorGUILayout.HelpBox(
                    $"Preserve structure after removing '{displayExclude}'",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Flat: all files to same folder", MessageType.Info);
            }
            EditorGUILayout.Space();

            // Auto-tag
            EditorGUILayout.BeginVertical("box");
            autoTagDependentAssets = EditorGUILayout.Toggle("Auto-Tag", autoTagDependentAssets);
            if (autoTagDependentAssets)
            {
                GUI.enabled = !isProcessingAutoTag && fileList.Count > 0;
                if (GUILayout.Button("Run Auto-Tag", GUILayout.Height(25)))
                {
                    ManualAutoTag();
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            showUsedOnly = GUILayout.Toggle(showUsedOnly, "Used Only", "Button");
            showUnusedOnly = GUILayout.Toggle(showUnusedOnly, "Unused Only", "Button");
            if (showUsedOnly && showUnusedOnly)
            {
                showUsedOnly = false;
                showUnusedOnly = false;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !isProcessingUsage && fileList.Count > 0;
            if (GUILayout.Button("Check Usage", GUILayout.Height(30)))
            {
                StartBatchUsageCheck();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Clear Cache", GUILayout.Width(100), GUILayout.Height(30)))
            {
                ClearCache();
            }
            EditorGUILayout.EndHorizontal();

            if (isProcessingUsage)
            {
                float progress = itemsToProcess.Count > 0 ? (float)processingIndex / itemsToProcess.Count : 0;
                EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true)), progress,
                    $"{processingIndex}/{itemsToProcess.Count}");
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Drag & Drop:", EditorStyles.boldLabel);
            DrawDragAndDropArea();

            dragDropHeight = DrawResizeSlider(dragDropHeight, "Drop Area Height");
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse Folder", GUILayout.Height(30)))
            {
                BrowseFolder();
            }
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear", "Remove all?", "Yes", "No"))
                {
                    fileList.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            showDependencies = EditorGUILayout.Toggle("Show Dependencies", showDependencies);
            EditorGUILayout.Space();

            if (fileList.Count > 0)
            {
                EditorGUILayout.LabelField("Files:", EditorStyles.boldLabel);
                DrawDetailedFileList();

                detailedListHeight = DrawResizeSlider(detailedListHeight, "List Height");
                EditorGUILayout.Space();
            }

            DrawDestinationSelector();
            EditorGUILayout.Space();

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Move Files", GUILayout.Height(40)))
            {
                MoveFiles();
            }
        }

        private void DrawFileTypeFilter()
        {
            EditorGUILayout.LabelField("File Type Filter:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            bool allSelected = selectedFileTypes == FileType.All;
            bool newAllSelected = EditorGUILayout.Toggle("All Types", allSelected);
            if (newAllSelected != allSelected)
            {
                selectedFileTypes = newAllSelected ? FileType.All : FileType.None;
            }

            EditorGUILayout.Space(5);
            selectedFileTypes = (FileType)EditorGUILayout.EnumFlagsField("Types:", selectedFileTypes);
            EditorGUILayout.EndVertical();
        }

        private void DrawScanSettings()
        {
            EditorGUILayout.BeginHorizontal();
            thoroughScan = EditorGUILayout.Toggle("Thorough Scan", thoroughScan);
            EditorGUILayout.EndHorizontal();

            if (thoroughScan)
            {
                EditorGUILayout.HelpBox("Checks all asset types (slower but complete)", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Only checks Scenes/Prefabs/ScriptableObjects (faster)", MessageType.Info);
            }

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            bool newScanAll = EditorGUILayout.Toggle("Scan Entire Project", scanAllFolders);
            if (newScanAll != scanAllFolders)
            {
                if (newScanAll)
                {
                    if (EditorUtility.DisplayDialog("Scan All?", "May be slow!", "OK", "Cancel"))
                    {
                        scanAllFolders = true;
                        ClearCache();
                    }
                }
                else
                {
                    scanAllFolders = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (scanAllFolders)
            {
                EditorGUILayout.HelpBox("Scanning entire project (may be very slow)", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Only scanning selected folders below", MessageType.Info);
            }

            GUI.enabled = !scanAllFolders;

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Folder", GUILayout.Height(25)))
            {
                AddScanFolder();
            }
            if (scanFolders.Count > 0 && GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(25)))
            {
                scanFolders.Clear();
                ClearCache();
            }
            EditorGUILayout.EndHorizontal();

            if (scanFolders.Count > 0)
            {
                // Dynamic height: 30px per folder, min 50, max 300
                float dynamicHeight = Mathf.Clamp(scanFolders.Count * 30f, 50f, 300f);

                scanFoldersScrollPosition = EditorGUILayout.BeginScrollView(scanFoldersScrollPosition, GUILayout.Height(dynamicHeight));

                for (int i = scanFolders.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Label(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(20), GUILayout.Height(20));
                    EditorGUILayout.LabelField(scanFolders[i], EditorStyles.miniLabel);

                    if (scanFolderCache.ContainsKey(scanFolders[i]))
                    {
                        GUILayout.Label($"({scanFolderCache[scanFolders[i]].Count})", EditorStyles.miniLabel, GUILayout.Width(50));
                    }

                    if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(20)))
                    {
                        string folder = scanFolders[i];
                        scanFolders.RemoveAt(i);
                        scanFolderCache.Remove(folder);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            GUI.enabled = true;
        }

        private void AddScanFolder()
        {
            string folder = EditorUtility.OpenFolderPanel("Select", "Assets", "");

            if (!string.IsNullOrEmpty(folder))
            {
                if (folder.StartsWith(Application.dataPath))
                {
                    folder = "Assets" + folder.Substring(Application.dataPath.Length);

                    if (!scanFolders.Contains(folder))
                    {
                        scanFolders.Add(folder);
                        CacheFolderContents(folder);
                    }
                }
            }
        }

        private void CacheFolderContents(string folderPath)
        {
            if (!scanFolderCache.ContainsKey(folderPath))
            {
                List<string> assets = BuildFolderCache(folderPath, thoroughScan);
                scanFolderCache[folderPath] = assets;
            }
        }

        private static List<string> BuildFolderCache(string folderPath, bool deepScan)
        {
            List<string> assetPaths = new List<string>();
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (deepScan)
                {
                    if (!path.EndsWith(".cs") && !path.EndsWith(".meta") && !path.EndsWith(".dll"))
                    {
                        assetPaths.Add(path);
                    }
                }
                else
                {
                    if (path.EndsWith(".unity") || path.EndsWith(".prefab") || path.EndsWith(".asset"))
                    {
                        assetPaths.Add(path);
                    }
                }
            }

            return assetPaths;
        }

        private void DrawDragAndDropArea()
        {
            Event evt = Event.current;
            EditorGUILayout.BeginVertical("box");

            Rect dropArea = GUILayoutUtility.GetRect(0.0f, dragDropHeight, GUILayout.ExpandWidth(true));

            if (fileList.Count == 0)
            {
                GUI.Box(dropArea, "Drop Files/Folders", EditorStyles.helpBox);
            }
            else
            {
                GUI.Box(dropArea, "", EditorStyles.helpBox);

                GUILayout.BeginArea(dropArea);
                Vector2 tempScrollPos = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(dragDropHeight - 5));

                for (int i = fileList.Count - 1; i >= 0; i--)
                {
                    AssetItem item = fileList[i];

                    EditorGUILayout.BeginHorizontal("box");

                    Texture2D icon = AssetDatabase.GetCachedIcon(item.path) as Texture2D;
                    if (icon != null)
                    {
                        GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
                    }

                    GUILayout.Label(item.fileName, GUILayout.Width(200));
                    GUILayout.Label($"[{item.type}]", EditorStyles.miniLabel, GUILayout.Width(80));
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        fileList.RemoveAt(i);
                        if (selectedItemForDetails == item)
                            selectedItemForDetails = null;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();

                scrollPosition = tempScrollPos;
            }

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object obj in DragAndDrop.objectReferences)
                        {
                            string path = AssetDatabase.GetAssetPath(obj);

                            if (Directory.Exists(path))
                            {
                                AddFilesFromFolder(path);
                            }
                            else if (File.Exists(path))
                            {
                                AddFile(path);
                            }
                        }
                    }
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.LabelField($"Total: {fileList.Count}", EditorStyles.miniLabel);
        }

        private void AddFile(string path)
        {
            if (!fileList.Any(f => f.path == path))
            {
                AssetItem item = new AssetItem(path, scanFolders, scanAllFolders, thoroughScan);

                if (autoExtractOnDrop)
                {
                    if ((selectedFileTypes & item.type) != 0 || selectedFileTypes == FileType.All)
                    {
                        fileList.Add(item);
                    }
                }
                else
                {
                    fileList.Add(item);
                }
            }
        }

        private void AddFilesFromFolder(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string path = file.Replace("\\", "/");
                if (!path.EndsWith(".meta"))
                {
                    AddFile(path);
                }
            }
        }

        private void BrowseFolder()
        {
            string folder = EditorUtility.OpenFolderPanel("Select", "Assets", "");

            if (!string.IsNullOrEmpty(folder))
            {
                if (folder.StartsWith(Application.dataPath))
                {
                    folder = "Assets" + folder.Substring(Application.dataPath.Length);
                    AddFilesFromFolder(folder);
                }
            }
        }

        private void DrawDetailedFileList()
        {
            List<AssetItem> list = GetFilteredFileList();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(detailedListHeight));

            for (int i = list.Count - 1; i >= 0; i--)
            {
                AssetItem item = list[i];

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();

                Texture2D icon = AssetDatabase.GetCachedIcon(item.path) as Texture2D;
                if (icon != null)
                {
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                }

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(item.fileName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(item.path, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                if (usageCache.ContainsKey(item.path))
                {
                    bool used = usageCache[item.path];
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.normal.textColor = used ? Color.green : Color.red;
                    style.fontStyle = FontStyle.Bold;

                    string text = used ? "USED" : "UNUSED";
                    if (used && item.isIndirectlyUsed)
                    {
                        text = "USED (Auto)";
                        style.normal.textColor = new Color(0.3f, 0.7f, 1f);
                    }

                    GUILayout.Label(text, style, GUILayout.Width(80));
                }
                else
                {
                    GUILayout.Label("UNCHECKED", EditorStyles.miniLabel, GUILayout.Width(80));
                }

                if (GUILayout.Button("Info", GUILayout.Width(50)))
                {
                    selectedItemForDetails = (selectedItemForDetails == item) ? null : item;
                    if (selectedItemForDetails == item)
                    {
                        item.EnsureDependenciesLoaded();
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (showDependencies || selectedItemForDetails == item)
                {
                    item.EnsureDependenciesLoaded();

                    if (item.dependencies.Count > 0)
                    {
                        EditorGUILayout.BeginVertical("helpbox");
                        EditorGUILayout.LabelField($"Dependencies ({item.dependencies.Count}):", EditorStyles.miniLabel);

                        foreach (string dep in item.dependencies)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            EditorGUILayout.LabelField("> " + Path.GetFileName(dep), EditorStyles.miniLabel);
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndVertical();
                    }

                    if (selectedItemForDetails == item && item.referencedBy.Count > 0)
                    {
                        EditorGUILayout.BeginVertical("helpbox");
                        EditorGUILayout.LabelField($"Referenced By ({item.referencedBy.Count}):", EditorStyles.miniLabel);
                        foreach (string refBy in item.referencedBy)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            EditorGUILayout.LabelField("< " + Path.GetFileName(refBy), EditorStyles.miniLabel);
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private List<AssetItem> GetFilteredFileList()
        {
            return fileList.Where(item =>
            {
                bool pass = true;
                if (showUsedOnly) pass = item.GetIsUsed();
                if (showUnusedOnly) pass = !item.GetIsUsed();
                return pass;
            }).ToList();
        }

        private void DrawDestinationSelector()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Destination:", GUILayout.Width(80));
            destinationPath = EditorGUILayout.TextField(destinationPath);

            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string folder = EditorUtility.OpenFolderPanel("Destination", "Assets", "");
                if (!string.IsNullOrEmpty(folder))
                {
                    if (folder.StartsWith(Application.dataPath))
                    {
                        destinationPath = "Assets" + folder.Substring(Application.dataPath.Length);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void MoveFiles()
        {
            usedFileList = GetFilteredFileList();
            if (usedFileList.Count == 0)
            {
                EditorUtility.DisplayDialog("No Files", "No files to move.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(destinationPath))
            {
                EditorUtility.DisplayDialog("Invalid", "Select destination.", "OK");
                return;
            }

            // Ensure destination exists
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
                AssetDatabase.Refresh(); // Refresh to ensure Unity sees the new folder
            }

            // Determine exclude path
            string pathToExclude = string.IsNullOrEmpty(excludePath) ? "Assets/" : excludePath;
            if (!pathToExclude.EndsWith("/"))
            {
                pathToExclude += "/";
            }

            Dictionary<string, string> fileMoveMap = new Dictionary<string, string>();
            HashSet<string> mainFiles = new HashSet<string>();

            // Process ALL files with preserve logic
            foreach (AssetItem item in usedFileList)
            {
                string newPath = CalculateNewPath(item.path, pathToExclude);
                fileMoveMap[item.path] = newPath;
                mainFiles.Add(item.path);
            }

            // Add dependencies
            if (moveDependencies)
            {
                HashSet<string> processed = new HashSet<string>();

                foreach (AssetItem item in usedFileList)
                {
                    item.EnsureDependenciesLoaded();
                    AddDependenciesToMoveMap(item, fileMoveMap, mainFiles, processed, pathToExclude);
                }
            }

            // Pre-create all necessary directories
            HashSet<string> dirsToCreate = new HashSet<string>();
            foreach (var kvp in fileMoveMap)
            {
                string destDir = Path.GetDirectoryName(kvp.Value);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    dirsToCreate.Add(destDir);
                }
            }

            // Create all directories first
            foreach (string dir in dirsToCreate)
            {
                Directory.CreateDirectory(dir);
                Debug.Log($"[AssetMover] Created directory: {dir}");
            }

            // Refresh AssetDatabase to ensure Unity sees all new directories
            if (dirsToCreate.Count > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[AssetMover] Created {dirsToCreate.Count} directories, refreshing AssetDatabase...");
            }

            // Confirm
            int mainCount = mainFiles.Count;
            int depCount = fileMoveMap.Count - mainCount;

            string msg = $"Move {mainCount} files";
            if (moveDependencies && depCount > 0)
            {
                msg += $" + {depCount} deps";
            }
            if (preserveDependencyFolders)
            {
                msg += $"\n\nPreserve structure (exclude '{pathToExclude}')";
            }
            msg += $"\n\nTo: {destinationPath}";

            if (!EditorUtility.DisplayDialog("Move?", msg, "Move", "Cancel"))
            {
                return;
            }

            // Move files
            int moved = 0;
            int skipped = 0;
            List<AssetItem> toRemove = new List<AssetItem>();

            foreach (var kvp in fileMoveMap)
            {
                string src = kvp.Key;
                string dest = kvp.Value;
                string name = Path.GetFileName(src);

                if (src == dest)
                {
                    Debug.LogWarning($"[AssetMover] Skip: {name} - same location");
                    skipped++;
                    continue;
                }

                if (File.Exists(dest) && dest != src)
                {
                    if (!EditorUtility.DisplayDialog("Exists", $"{name} exists. Overwrite?", "Yes", "Skip"))
                    {
                        skipped++;
                        continue;
                    }
                }

                string error = AssetDatabase.MoveAsset(src, dest);

                if (string.IsNullOrEmpty(error))
                {
                    moved++;
                    Debug.Log($"[AssetMover] Moved: {name} -> {dest}");

                    if (mainFiles.Contains(src))
                    {
                        AssetItem item = usedFileList.FirstOrDefault(i => i.path == src);
                        if (item != null)
                        {
                            toRemove.Add(item);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[AssetMover] Failed: {name} - {error}");
                    skipped++;
                }
            }

            foreach (AssetItem item in toRemove)
            {
                fileList.Remove(item);
            }

            AssetDatabase.Refresh();

            string result = $"Moved {moved} files";
            if (skipped > 0)
            {
                result += $"\nSkipped {skipped} files";
            }

            EditorUtility.DisplayDialog("Done", result, "OK");
        }

        // Calculate new path with exclude logic
        private string CalculateNewPath(string sourcePath, string pathToExclude)
        {
            if (!preserveDependencyFolders)
            {
                // Flat
                string name = Path.GetFileName(sourcePath);
                return Path.Combine(destinationPath, name).Replace("\\", "/");
            }

            // Preserve with exclude
            string relative = sourcePath;

            if (sourcePath.StartsWith(pathToExclude))
            {
                relative = sourcePath.Substring(pathToExclude.Length);
            }
            else if (sourcePath.StartsWith("Assets/"))
            {
                relative = sourcePath.Substring("Assets/".Length);
            }

            string newPath = Path.Combine(destinationPath, relative).Replace("\\", "/");
            return newPath;
        }

        private void AddDependenciesToMoveMap(AssetItem item, Dictionary<string, string> map, HashSet<string> main, HashSet<string> processed, string exclude)
        {
            foreach (string dep in item.dependencies)
            {
                if (processed.Contains(dep))
                    continue;

                processed.Add(dep);

                if (!map.ContainsKey(dep))
                {
                    string newPath = CalculateNewPath(dep, exclude);
                    map[dep] = newPath;

                    AssetItem depItem = new AssetItem(dep, scanFolders, scanAllFolders, thoroughScan);
                    depItem.EnsureDependenciesLoaded();
                    AddDependenciesToMoveMap(depItem, map, main, processed, exclude);
                }
            }
        }
    }
}