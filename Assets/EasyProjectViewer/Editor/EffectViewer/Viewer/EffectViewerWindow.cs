using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Effect Viewer editor window
    /// Displays a grid of static thumbnails for prefabs containing ParticleSystem
    /// Double-click to open preview window
    /// </summary>
    public class EffectViewerWindow : EditorWindow
    {
        // UI Configuration
        private const int THUMBNAIL_SIZE = 128;
        private const int THUMBNAIL_PADDING = 10;
        private const int STATUSBAR_HEIGHT = 20;

        // Drag and Drop
        private const string DRAG_ID = "EffectViewerDrag";

        // Serialized GUID list (persists across domain reload)
        [SerializeField]
        private List<string> serializedEffectGuids = new List<string>();

        // Data
        private List<EffectPrefabData> allEffectPrefabs = new List<EffectPrefabData>();
        private List<EffectPrefabData> filteredEffectPrefabs = new List<EffectPrefabData>();

        // UI State
        private Vector2 scrollPosition;
        private string searchText = "";
        private bool isScanning = false;
        private HashSet<EffectPrefabData> selectedEffects = new HashSet<EffectPrefabData>(); // Currently selected effects
        private int lastClickedIndex = -1; // For Shift+click range selection
        private EffectPrefabData pendingSelectionEffect = null; // Deferred selection for drag support
        private bool isDragging = false; // Drag in progress flag

        // Tag Related
        private string selectedTag = null; // Currently selected tag (null = show all)
        private Vector2 tagScrollPosition;
        private float sidebarWidth = 200f;
        private const float SIDEBAR_MIN_WIDTH = 100f;
        private const float SIDEBAR_MAX_WIDTH = 400f;
        private const float RESIZER_WIDTH = 5f;
        private bool isResizingSidebar = false;
        private bool isTagPaneVisible = true; // Tag pane visibility state

        // Styles
        private GUIStyle thumbnailBoxStyle;
        private GUIStyle selectedThumbnailStyle;
        private GUIStyle thumbnailLabelStyle;
        private GUIStyle tagButtonStyle;
        private GUIStyle selectedTagButtonStyle;
        private GUIStyle dragHoverTagButtonStyle;

        // Thumbnail Generation
        private EffectThumbnailGenerator thumbnailGenerator;
        private bool isGeneratingThumbnails = false;
        private int thumbnailGenerationIndex = 0;
        private bool forceRegenerate = false;

        // Cached tag counts (rebuilt when data changes, not every frame)
        private Dictionary<string, int> cachedTagCounts = new Dictionary<string, int>();
        private bool tagCountsDirty = true;

        // Cached effect type icons (loaded once, reused every frame)
        private static Texture2D s_ParticleSystemIcon;
        private static Texture2D s_VFXGraphIcon;
        private static Texture2D s_ManualAddIcon;
        private static bool s_IconsCached;

        [MenuItem("Tools/EffectViewer/Viewer %#e")]
        public static void ShowWindow()
        {
            var window = GetWindow<EffectViewerWindow>("Effect Viewer");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        [MenuItem("Tools/EffectViewer/Write a Review")]
        public static void OpenReviewPage()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/slug/348954");
        }

        private void OnEnable()
        {
            // Initialize tag management system
            EffectTagManager.Initialize();

            // Initialize thumbnail generator
            if (thumbnailGenerator == null)
            {
                thumbnailGenerator = new EffectThumbnailGenerator();
                thumbnailGenerator.Initialize();
            }

            // Delay initial scan until after GUI initialization
            // Skip scan if data already exists (performance optimization)
            if (allEffectPrefabs.Count == 0)
            {
                if (serializedEffectGuids.Count > 0)
                {
                    // After domain reload: restore from GUIDs (lightweight)
                    EditorApplication.delayCall += () =>
                    {
                        if (this != null)
                        {
                            RestoreFromGuids();
                        }
                    };
                }
                else
                {
                    // No data: full scan (first time or window reopened)
                    EditorApplication.delayCall += () =>
                    {
                        if (this != null)
                        {
                            ScanEffectPrefabs();
                        }
                    };
                }
            }
            else
            {
                // Apply filter to restore view state
                ApplyFilter();
            }

            // Register update loop
            EditorApplication.update += OnEditorUpdate;

            // Register drop handlers for Hierarchy and SceneView
#if UNITY_6000_3_OR_NEWER
            DragAndDrop.AddDropHandlerV2(OnHierarchyDrop);
#else
            DragAndDrop.AddDropHandler(OnHierarchyDrop);
#endif
            SceneView.duringSceneGui += OnSceneViewGUI;
        }

        private void OnDisable()
        {
            // Unregister update loop
            EditorApplication.update -= OnEditorUpdate;

            // Unregister drop handlers
#if UNITY_6000_3_OR_NEWER
            DragAndDrop.RemoveDropHandlerV2(OnHierarchyDrop);
#else
            DragAndDrop.RemoveDropHandler(OnHierarchyDrop);
#endif
            SceneView.duringSceneGui -= OnSceneViewGUI;

            // Cleanup resources
            CleanupResources();

            // Cleanup thumbnail generator
            if (thumbnailGenerator != null)
            {
                thumbnailGenerator.Cleanup();
                thumbnailGenerator = null;
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            DrawToolbar();

            // Split layout (left and right)
            EditorGUILayout.BeginHorizontal();
            {
                // Left: Tag list (only when visible)
                if (isTagPaneVisible)
                {
                    DrawTagList();

                    // Resizer
                    DrawResizer();
                }

                // Right: Effect grid
                DrawGridView();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Editor update (static thumbnail generation only)
        /// </summary>
        private void OnEditorUpdate()
        {
            // Static thumbnail generation (one at a time)
            if (isGeneratingThumbnails && allEffectPrefabs.Count > 0)
            {
                // Skip effects that already have thumbnails (unless force regenerate is enabled)
                while (thumbnailGenerationIndex < allEffectPrefabs.Count)
                {
                    var effect = allEffectPrefabs[thumbnailGenerationIndex];

                    // Check if thumbnail needs generation
                    bool needsGeneration = forceRegenerate || effect.StaticThumbnail == null;

                    if (needsGeneration)
                    {
                        GenerateStaticThumbnailForEffect(effect);
                        thumbnailGenerationIndex++;
                        Repaint();
                        break; // Process one at a time
                    }
                    else
                    {
                        // Skip this effect (already has thumbnail)
                        thumbnailGenerationIndex++;
                    }
                }

                // Check if generation is complete
                if (thumbnailGenerationIndex >= allEffectPrefabs.Count)
                {
                    isGeneratingThumbnails = false;
                    thumbnailGenerationIndex = 0;
                    forceRegenerate = false;
                }
            }
        }

        /// <summary>
        /// Generate static thumbnail
        /// </summary>
        private void GenerateStaticThumbnailForEffect(EffectPrefabData effect)
        {
            if (effect == null || effect.Prefab == null || thumbnailGenerator == null)
                return;

            // Skip if thumbnail already exists (unless force regenerate is enabled)
            if (!forceRegenerate && effect.StaticThumbnail != null)
            {
                return;
            }

#if VFX_GRAPH_AVAILABLE
            // VFX Graph requires multi-frame GPU warmup — use deferred capture
            if (effect.Type == EffectType.VFXGraph)
            {
                VFXDeferredThumbnailCapture.Enqueue(effect.Guid, effect.AssetPath, effect.Prefab, THUMBNAIL_SIZE);
                return;
            }
#endif

            effect.StaticThumbnail = thumbnailGenerator.GenerateStaticThumbnail(
                effect.Prefab,
                THUMBNAIL_SIZE,
                THUMBNAIL_SIZE
            );

            // Save generated thumbnail to cache
            if (effect.StaticThumbnail != null)
            {
                EffectThumbnailManager.SaveThumbnail(effect.Guid, effect.StaticThumbnail);
            }
        }

        /// <summary>
        /// Initialize styles
        /// </summary>
        private void InitializeStyles()
        {
            if (thumbnailBoxStyle == null)
            {
                thumbnailBoxStyle = EffectViewerStyles.CreateThumbnailBoxStyle();
            }

            if (selectedThumbnailStyle == null)
            {
                selectedThumbnailStyle = EffectViewerStyles.CreateSelectedThumbnailStyle();
            }

            if (thumbnailLabelStyle == null)
            {
                thumbnailLabelStyle = EffectViewerStyles.CreateThumbnailLabelStyle();
            }

            if (tagButtonStyle == null)
            {
                tagButtonStyle = EffectViewerStyles.CreateTagButtonStyle();
            }

            if (selectedTagButtonStyle == null)
            {
                selectedTagButtonStyle = EffectViewerStyles.CreateSelectedTagButtonStyle();
            }

            if (dragHoverTagButtonStyle == null)
            {
                dragHoverTagButtonStyle = EffectViewerStyles.CreateDragHoverTagButtonStyle();
            }
        }

        /// <summary>
        /// Draw toolbar
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Tag visibility toggle button (leftmost)
            string tagTooltip = isTagPaneVisible ? "Hide Tags" : "Show Tags";
            GUIContent tagIcon = EditorGUIUtility.IconContent("d_FilterByLabel", $"|{tagTooltip}");
            Color originalBgColor = GUI.backgroundColor;
            if (isTagPaneVisible)
            {
                GUI.backgroundColor = new Color(0.5f, 0.8f, 1f, 1f); // Light blue when active
            }
            if (GUILayout.Button(tagIcon, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                isTagPaneVisible = !isTagPaneVisible;
            }
            GUI.backgroundColor = originalBgColor;

            // Search box
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            string newSearchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                ApplyFilter();
            }

            GUILayout.FlexibleSpace();

            // Generation progress display
            if (isGeneratingThumbnails)
            {
                EditorGUILayout.LabelField(
                    $"Generating: {thumbnailGenerationIndex}/{allEffectPrefabs.Count}",
                    EditorStyles.miniLabel,
                    GUILayout.Width(150)
                );
            }

            // Refresh button
            EditorGUI.BeginDisabledGroup(isScanning || isGeneratingThumbnails);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                ScanEffectPrefabs();
            }
            EditorGUI.EndDisabledGroup();

            // Regenerate all thumbnails button
            EditorGUI.BeginDisabledGroup(isScanning || isGeneratingThumbnails || allEffectPrefabs.Count == 0);
            if (GUILayout.Button("Regen Thumbs", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                RegenerateAllThumbnails();
            }
            EditorGUI.EndDisabledGroup();

            // Viewer Settings button
            GUIContent viewerSettingsIcon = EditorGUIUtility.IconContent("_Popup", "|Viewer Settings");
            if (GUILayout.Button(viewerSettingsIcon, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                EffectViewerSettings.OpenSettings();
            }

            // Effect count display
            EditorGUILayout.LabelField(
                $"{filteredEffectPrefabs.Count} / {allEffectPrefabs.Count} effects",
                EditorStyles.miniLabel,
                GUILayout.Width(120)
            );

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw sidebar resizer
        /// </summary>
        private void DrawResizer()
        {
            // Use ExpandHeight instead of Screen.height (which is the OS screen
            // height, not the window's). Screen.height made BeginHorizontal grow
            // to monitor height, pushing the scroll view past the window bottom.
            Rect resizerRect = GUILayoutUtility.GetRect(
                RESIZER_WIDTH, 0,
                GUILayout.Width(RESIZER_WIDTH),
                GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(resizerRect, MouseCursor.ResizeHorizontal);

            // Visual representation of resizer
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(resizerRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            }

            // Mouse event handling
            if (Event.current.type == EventType.MouseDown && resizerRect.Contains(Event.current.mousePosition))
            {
                isResizingSidebar = true;
                Event.current.Use();
            }

            if (isResizingSidebar)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    sidebarWidth += Event.current.delta.x;
                    sidebarWidth = Mathf.Clamp(sidebarWidth, SIDEBAR_MIN_WIDTH, SIDEBAR_MAX_WIDTH);
                    Repaint();
                    Event.current.Use();
                }

                if (Event.current.type == EventType.MouseUp)
                {
                    isResizingSidebar = false;
                    Event.current.Use();
                }
            }
        }

        /// <summary>
        /// Draw tag list
        /// </summary>
        private void DrawTagList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(sidebarWidth));
            tagScrollPosition = EditorGUILayout.BeginScrollView(tagScrollPosition);

            // "All Effects" root node
            bool isAllSelected = string.IsNullOrEmpty(selectedTag);
            GUIStyle allButtonStyle = isAllSelected ? selectedTagButtonStyle : tagButtonStyle;

            if (GUILayout.Button($"📂 All Effects ({allEffectPrefabs.Count})", allButtonStyle,
                GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                selectedTag = null;
                ApplyFilter();
            }

            EditorGUILayout.Space(8);

            // Tag list (use cached counts to avoid O(tags × effects) per frame)
            if (tagCountsDirty)
            {
                cachedTagCounts.Clear();
                var allTags = EffectTagManager.GetAllTags();
                foreach (var t in allTags)
                    cachedTagCounts[t] = EffectTagManager.GetEffectCountForTag(allEffectPrefabs, t);
                tagCountsDirty = false;
            }

            var tags = EffectTagManager.GetAllTags();
            foreach (var tag in tags)
            {
                int count = cachedTagCounts.TryGetValue(tag, out int c) ? c : 0;
                bool isSelected = (selectedTag == tag);

                // Tag display
                EditorGUILayout.BeginHorizontal();
                Rect buttonRect = GUILayoutUtility.GetRect(
                    new GUIContent($"🏷️ {tag} ({count})"),
                    tagButtonStyle,
                    GUILayout.Height(30),
                    GUILayout.ExpandWidth(true)
                );

                // Check if drag hovering
                bool isDragHover = false;
                if (Event.current.type == EventType.DragUpdated && buttonRect.Contains(Event.current.mousePosition))
                {
                    var draggedEffects = DragAndDrop.GetGenericData(DRAG_ID) as List<EffectPrefabData>;
                    if (draggedEffects != null && draggedEffects.Count > 0)
                    {
                        isDragHover = true;
                    }
                }

                // Determine style (priority: drag hover > selected > normal)
                GUIStyle buttonStyle = isDragHover ? dragHoverTagButtonStyle :
                                       isSelected ? selectedTagButtonStyle :
                                       tagButtonStyle;

                // Drag and drop handling
                if (Event.current.type == EventType.DragUpdated && buttonRect.Contains(Event.current.mousePosition))
                {
                    // Check if drag data is valid
                    var draggedEffects = DragAndDrop.GetGenericData(DRAG_ID) as List<EffectPrefabData>;
                    if (draggedEffects != null && draggedEffects.Count > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }
                }

                if (Event.current.type == EventType.DragPerform && buttonRect.Contains(Event.current.mousePosition))
                {
                    // Execute drop - add tag to all dragged effects
                    var draggedEffects = DragAndDrop.GetGenericData(DRAG_ID) as List<EffectPrefabData>;
                    if (draggedEffects != null && draggedEffects.Count > 0)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var effect in draggedEffects)
                        {
                            EffectTagManager.AddTagToEffect(effect.Guid, tag);
                        }
                        ApplyFilter();
                        Event.current.Use();
                    }
                }

                // Right-click menu (detect right button in MouseDown event)
                if (Event.current.type == EventType.MouseDown &&
                    Event.current.button == 1 &&
                    buttonRect.Contains(Event.current.mousePosition))
                {
                    ShowTagContextMenu(tag);
                    Event.current.Use();
                }

                // Left-click handling
                if (GUI.Button(buttonRect, $"🏷️ {tag} ({count})", buttonStyle))
                {
                    selectedTag = tag;
                    ApplyFilter();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2); // Space between tags
            }

            EditorGUILayout.Space(10);

            // "+ New Tag" button
            GUIStyle newTagButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 8, 8)
            };

            if (GUILayout.Button("+ New Tag", newTagButtonStyle, GUILayout.Height(32)))
            {
                TagCreateDialog.ShowDialog(() => Repaint());
            }

            EditorGUILayout.Space(5);

            // "Reset All Tags" button
            GUIStyle resetButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                padding = new RectOffset(10, 10, 6, 6),
                normal = { textColor = new Color(1f, 0.5f, 0.5f) }
            };

            if (GUILayout.Button("Reset All Tags", resetButtonStyle, GUILayout.Height(28)))
            {
                ResetAllTagsWithConfirmation();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Tag right-click context menu
        /// </summary>
        private void ShowTagContextMenu(string tag)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(
                new GUIContent("Rename Tag"),
                false,
                () => TagRenameDialog.ShowDialog(tag, () =>
                {
                    // Update selected tag after rename
                    if (selectedTag == tag)
                    {
                        selectedTag = null;
                    }
                    Repaint();
                })
            );

            menu.AddSeparator("");

            menu.AddItem(
                new GUIContent("Delete Tag"),
                false,
                () => DeleteTagWithConfirmation(tag)
            );

            menu.ShowAsContext();
        }

        /// <summary>
        /// Tag deletion confirmation dialog
        /// </summary>
        private void DeleteTagWithConfirmation(string tag)
        {
            int effectCount = EffectTagManager.GetEffectCountForTag(allEffectPrefabs, tag);
            string message = effectCount > 0
                ? $"Are you sure you want to delete the tag '{tag}'?\n\nThis will remove the tag from {effectCount} effect(s)."
                : $"Are you sure you want to delete the tag '{tag}'?";

            if (EditorUtility.DisplayDialog(
                "Delete Tag",
                message,
                "Delete",
                "Cancel"))
            {
                EffectTagManager.RemoveTag(tag);
                if (selectedTag == tag)
                {
                    selectedTag = null;
                }
                ApplyFilter();
            }
        }

        /// <summary>
        /// Reset all tags confirmation dialog
        /// </summary>
        private void ResetAllTagsWithConfirmation()
        {
            var allTags = EffectTagManager.GetAllTags();
            if (allTags.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Reset All Tags",
                    "There are no tags to reset.",
                    "OK");
                return;
            }

            int totalEffectsWithTags = allEffectPrefabs.Count(e =>
                EffectTagManager.GetEffectTags(e.Guid).Count > 0);

            string message = $"Are you sure you want to reset ALL tags?\n\n" +
                           $"This will delete:\n" +
                           $"• {allTags.Count} tag(s)\n" +
                           $"• All tag assignments from {totalEffectsWithTags} effect(s)\n\n" +
                           $"This action cannot be undone.";

            if (EditorUtility.DisplayDialog(
                "Reset All Tags",
                message,
                "Reset All",
                "Cancel"))
            {
                EffectTagManager.ResetDatabase();
                selectedTag = null;
                ApplyFilter();
            }
        }

        /// <summary>
        /// Effect right-click context menu (supports multi-selection)
        /// </summary>
        private void ShowEffectContextMenu(EffectPrefabData clickedEffect)
        {
            // If clicked effect is not in selection, select only that effect
            if (!selectedEffects.Contains(clickedEffect))
            {
                selectedEffects.Clear();
                selectedEffects.Add(clickedEffect);
            }

            // Create a copy for lambda capture (use ToList for explicit copy)
            var targetEffects = selectedEffects.ToList();
            int selectionCount = targetEffects.Count;
            bool isMultiSelect = selectionCount > 1;

            GenericMenu menu = new GenericMenu();
            var allTags = EffectTagManager.GetAllTags();

            // Add Tag submenu
            if (allTags.Count > 0)
            {
                foreach (var tag in allTags)
                {
                    string tagName = tag; // Local copy (for lambda capture)
                    var effectsCopy = targetEffects; // Explicit capture
                    menu.AddItem(
                        new GUIContent($"Add Tag/{tag}"),
                        false,
                        () =>
                        {
                            foreach (var effect in effectsCopy)
                                EffectTagManager.AddTagToEffect(effect.Guid, tagName);
                            Repaint();
                        }
                    );
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Add Tag/No tags available"));
            }

            menu.AddSeparator("Add Tag/");
            menu.AddItem(
                new GUIContent("Add Tag/+ Create New Tag..."),
                false,
                () => ShowCreateTagDialogForEffects(targetEffects)
            );

            // Remove Tag submenu (collect all tags from selected effects)
            var allSelectedTags = GetAllTagsFromEffects(targetEffects);
            if (allSelectedTags.Count > 0)
            {
                menu.AddSeparator("");
                foreach (var tag in allSelectedTags)
                {
                    string tagName = tag; // Local copy (for lambda capture)
                    var effectsCopy = targetEffects; // Explicit capture
                    menu.AddItem(
                        new GUIContent($"Remove Tag/{tag}"),
                        false,
                        () =>
                        {
                            foreach (var effect in effectsCopy)
                                EffectTagManager.RemoveTagFromEffect(effect.Guid, tagName);
                            ApplyFilter();
                        }
                    );
                }
            }

            // Instantiate at Origin (supports multi-selection)
            menu.AddSeparator("");
            string instantiateLabel = isMultiSelect
                ? $"Instantiate {selectionCount} items at Origin"
                : "Instantiate at Origin";
            menu.AddItem(
                new GUIContent(instantiateLabel),
                false,
                () =>
                {
                    foreach (var effect in targetEffects)
                        InstantiateEffect(effect);
                }
            );

            // Single selection only options
            if (!isMultiSelect)
            {
                menu.AddSeparator("");
                menu.AddItem(
                    new GUIContent("Open in Project"),
                    false,
                    () => EditorGUIUtility.PingObject(clickedEffect.Prefab)
                );
            }

            // Regenerate Thumbnail (supports multi-selection)
            menu.AddSeparator("");
            string regenLabel = isMultiSelect
                ? $"Regenerate {selectionCount} Thumbnails"
                : "Regenerate Auto Thumbnail";
            menu.AddItem(
                new GUIContent(regenLabel),
                false,
                () => RegenerateThumbnailsForEffects(targetEffects)
            );

            // Remove from Viewer (supports multi-selection)
            menu.AddSeparator("");
            string removeLabel = isMultiSelect
                ? $"Remove {selectionCount} items from Viewer"
                : "Remove from Viewer";
            menu.AddItem(
                new GUIContent(removeLabel),
                false,
                () => RemoveFromViewer(targetEffects)
            );

            menu.ShowAsContext();
        }

        /// <summary>
        /// Get all tags from multiple effects (union)
        /// </summary>
        private HashSet<string> GetAllTagsFromEffects(List<EffectPrefabData> effects)
        {
            var result = new HashSet<string>();
            foreach (var effect in effects)
            {
                foreach (var tag in EffectTagManager.GetEffectTags(effect.Guid))
                {
                    result.Add(tag);
                }
            }
            return result;
        }

        /// <summary>
        /// Instantiate effect at scene origin
        /// </summary>
        private void InstantiateEffect(EffectPrefabData effect)
        {
            if (effect == null || effect.Prefab == null)
            {
                Debug.LogWarning("Cannot instantiate: Invalid effect or prefab.");
                return;
            }

            // Instantiate prefab in scene
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(effect.Prefab);
            if (instance == null)
            {
                Debug.LogError($"Failed to instantiate prefab: {effect.Name}");
                return;
            }

            // Undo support (deletable with Ctrl+Z)
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Effect");

            // Set as selected object
            Selection.activeGameObject = instance;

            // Highlight in Hierarchy view
            EditorGUIUtility.PingObject(instance);

            Debug.Log($"Instantiated '{effect.Name}'");
        }

        /// <summary>
        /// Handle drop on Hierarchy window - instantiate at origin
        /// </summary>
#if UNITY_6000_3_OR_NEWER
        private DragAndDropVisualMode OnHierarchyDrop(
            UnityEngine.EntityId dropTargetEntityId,
            HierarchyDropFlags dropMode,
            Transform parentForDraggedObjects,
            bool perform)
#else
        private DragAndDropVisualMode OnHierarchyDrop(
            int dropTargetInstanceID,
            HierarchyDropFlags dropMode,
            Transform parentForDraggedObjects,
            bool perform)
#endif
        {
            // Check if this is our custom drag data (now a list)
            var effects = DragAndDrop.GetGenericData(DRAG_ID) as List<EffectPrefabData>;
            if (effects == null || effects.Count == 0)
            {
                // Not our drag, let Unity handle it
                return DragAndDropVisualMode.None;
            }

            if (perform)
            {
                // Perform the drop - instantiate all effects at origin
                foreach (var effect in effects)
                {
                    InstantiateEffect(effect);
                }
            }

            return DragAndDropVisualMode.Copy;
        }

        /// <summary>
        /// Handle drop on SceneView - instantiate at origin
        /// </summary>
        private void OnSceneViewGUI(SceneView sceneView)
        {
            Event evt = Event.current;

            // Handle drag update
            if (evt.type == EventType.DragUpdated)
            {
                var effects = DragAndDrop.GetGenericData(DRAG_ID) as List<EffectPrefabData>;
                if (effects != null && effects.Count > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                }
            }
            // Handle drag perform (drop)
            else if (evt.type == EventType.DragPerform)
            {
                var effects = DragAndDrop.GetGenericData(DRAG_ID) as List<EffectPrefabData>;
                if (effects != null && effects.Count > 0)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var effect in effects)
                    {
                        InstantiateEffect(effect);
                    }
                    evt.Use();
                }
            }
        }

        /// <summary>
        /// Toggle effect tag (add/remove)
        /// </summary>
        private void ToggleEffectTag(EffectPrefabData effect, string tag)
        {
            var effectTags = EffectTagManager.GetEffectTags(effect.Guid);
            if (effectTags.Contains(tag))
            {
                EffectTagManager.RemoveTagFromEffect(effect.Guid, tag);
            }
            else
            {
                EffectTagManager.AddTagToEffect(effect.Guid, tag);
            }
            ApplyFilter();
        }

        /// <summary>
        /// Show tag creation dialog for effect
        /// </summary>
        private void ShowCreateTagDialogForEffect(EffectPrefabData effect)
        {
            ShowCreateTagDialogForEffects(new List<EffectPrefabData> { effect });
        }

        /// <summary>
        /// Show tag creation dialog for multiple effects
        /// </summary>
        private void ShowCreateTagDialogForEffects(List<EffectPrefabData> effects)
        {
            TagCreateDialog.ShowDialog(() =>
            {
                // After tag creation, add that tag to all effects
                var newTags = EffectTagManager.GetAllTags();
                if (newTags.Count > 0)
                {
                    string newTag = newTags[newTags.Count - 1]; // Last added tag
                    foreach (var effect in effects)
                    {
                        EffectTagManager.AddTagToEffect(effect.Guid, newTag);
                    }
                }
                Repaint();
            });
        }

        /// <summary>
        /// Draw grid view
        /// </summary>
        private void DrawGridView()
        {
            // Calculate grid width (use full width when tag pane is hidden)
            float rightPaneWidth = isTagPaneVisible
                ? position.width - sidebarWidth - RESIZER_WIDTH
                : position.width;

            // Fix right pane width
            EditorGUILayout.BeginVertical(GUILayout.Width(rightPaneWidth));

            // Status bar (selected prefab path) - above scroll view
            DrawStatusBar();

            if (filteredEffectPrefabs == null || filteredEffectPrefabs.Count == 0)
            {
                DrawEmptyState();
                EditorGUILayout.EndVertical();
                return;
            }

            int columns = Mathf.Max(1, (int)(rightPaneWidth / (THUMBNAIL_SIZE + THUMBNAIL_PADDING * 2)));

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            int currentColumn = 0;
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < filteredEffectPrefabs.Count; i++)
            {
                var effect = filteredEffectPrefabs[i];

                // Start new row
                if (currentColumn >= columns)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    currentColumn = 0;
                }

                DrawThumbnail(effect);
                currentColumn++;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw thumbnail (static image only, double-click to show preview)
        /// </summary>
        private void DrawThumbnail(EffectPrefabData effect)
        {
            EditorGUILayout.BeginVertical(thumbnailBoxStyle, GUILayout.Width(THUMBNAIL_SIZE), GUILayout.Height(THUMBNAIL_SIZE + 42));

            // Tag display (top of thumbnail, max 2 tags)
            var tags = EffectTagManager.GetEffectTags(effect.Guid);
            if (tags.Count > 0)
            {
                string tagText = string.Join(" / ", tags.Take(2));
                if (tags.Count > 2)
                {
                    tagText += $" +{tags.Count - 2}";
                }

                // Get Unity tag icon
                GUIContent tagContent = new(tagText, EditorGUIUtility.IconContent("d_FilterByLabel").image);

                GUIStyle tagStyle = new GUIStyle(thumbnailLabelStyle)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.7f, 0.9f, 1f) }
                };
                EditorGUILayout.LabelField(tagContent, tagStyle, GUILayout.Height(12));
            }
            else
            {
                // Reserve same height space when no tags
                GUILayout.Space(12);
            }

            // Thumbnail image
            Rect thumbnailRect = GUILayoutUtility.GetRect(THUMBNAIL_SIZE, THUMBNAIL_SIZE);

            // Display static thumbnail
            if (effect.StaticThumbnail != null)
            {
                GUI.DrawTexture(thumbnailRect, effect.StaticThumbnail, ScaleMode.ScaleToFit);
            }
            else
            {
                // When thumbnail hasn't been generated yet
                EditorGUI.DrawRect(thumbnailRect, new Color(0.2f, 0.2f, 0.2f));
                GUI.Label(thumbnailRect, "Loading...", thumbnailLabelStyle);
            }

            // Effect type icon display (top left)
            if (effect.Type != EffectType.Unknown)
            {
                Texture2D iconTexture = GetCachedEffectIcon(effect.Type);

                if (iconTexture != null)
                {
                    const int iconSize = 24;
                    Rect iconRect = new Rect(thumbnailRect.x + 4, thumbnailRect.y + 4, iconSize, iconSize);

                    // Semi-transparent background
                    EditorGUI.DrawRect(iconRect, new Color(0f, 0f, 0f, 0.6f));

                    // Draw icon
                    GUI.DrawTexture(iconRect, iconTexture, ScaleMode.ScaleToFit);
                }
            }

            // Manual addition indicator (top right)
            if (effect.IsManuallyAdded)
            {
                const int iconSize = 20;
                Rect manualIconRect = new Rect(
                    thumbnailRect.x + thumbnailRect.width - iconSize - 4,
                    thumbnailRect.y + 4,
                    iconSize,
                    iconSize
                );

                // Semi-transparent background
                EditorGUI.DrawRect(manualIconRect, new Color(0f, 0f, 0f, 0.7f));

                // Use cached manual-add icon
                var manualIconTex = GetCachedManualAddIcon();
                if (manualIconTex != null)
                {
                    GUI.DrawTexture(manualIconRect, manualIconTex, ScaleMode.ScaleToFit);
                }

                // Tooltip
                if (manualIconRect.Contains(Event.current.mousePosition))
                {
                    GUI.Label(manualIconRect, new GUIContent("", "Manually Added"));
                }
            }

            // Click event (left mouse button only - button 0)
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                thumbnailRect.Contains(Event.current.mousePosition))
            {
                // Double click: Open Prefab Mode
                if (Event.current.clickCount == 2)
                {
                    OpenPrefabMode(effect);
                    Event.current.Use();
                    EditorGUILayout.EndVertical();
                    return;
                }

                int currentIndex = filteredEffectPrefabs.IndexOf(effect);

                if (Event.current.control)
                {
                    // Ctrl+click: Toggle selection
                    if (selectedEffects.Contains(effect))
                        selectedEffects.Remove(effect);
                    else
                        selectedEffects.Add(effect);
                    pendingSelectionEffect = null;
                }
                else if (Event.current.shift && lastClickedIndex >= 0)
                {
                    // Shift+click: Range selection
                    int start = Mathf.Min(lastClickedIndex, currentIndex);
                    int end = Mathf.Max(lastClickedIndex, currentIndex);
                    for (int j = start; j <= end; j++)
                    {
                        selectedEffects.Add(filteredEffectPrefabs[j]);
                    }
                    pendingSelectionEffect = null;
                }
                else if (selectedEffects.Contains(effect))
                {
                    // Clicked on already selected item - defer selection change for drag support
                    pendingSelectionEffect = effect;
                }
                else
                {
                    // Normal click on unselected item: Single selection
                    selectedEffects.Clear();
                    selectedEffects.Add(effect);
                    pendingSelectionEffect = null;
                }

                lastClickedIndex = currentIndex;
                isDragging = false;

                // Sync with Project view (multi-selection)
                Selection.objects = selectedEffects
                    .Where(e => e.Prefab != null)
                    .Select(e => e.Prefab as UnityEngine.Object)
                    .ToArray();

                Event.current.Use();
                Repaint();
            }

            // Mouse up - handle deferred selection (clicked but didn't drag)
            if (Event.current.type == EventType.MouseUp &&
                Event.current.button == 0 &&
                thumbnailRect.Contains(Event.current.mousePosition))
            {
                if (!isDragging && pendingSelectionEffect != null && pendingSelectionEffect == effect)
                {
                    // Didn't drag, so apply deferred single selection
                    selectedEffects.Clear();
                    selectedEffects.Add(effect);

                    Selection.objects = selectedEffects
                        .Where(e => e.Prefab != null)
                        .Select(e => e.Prefab as UnityEngine.Object)
                        .ToArray();

                    Repaint();
                }
                pendingSelectionEffect = null;
                isDragging = false;
            }

            // Start drag (supports both tag assignment and hierarchy/scene drop)
            if (Event.current.type == EventType.MouseDrag && thumbnailRect.Contains(Event.current.mousePosition))
            {
                isDragging = true;
                pendingSelectionEffect = null; // Cancel deferred selection

                DragAndDrop.PrepareStartDrag();

                // If the dragged effect is in selection, drag all selected effects
                // Otherwise, drag only the current effect
                List<EffectPrefabData> effectsToDrag;
                if (selectedEffects.Contains(effect) && selectedEffects.Count > 1)
                {
                    effectsToDrag = selectedEffects.ToList();
                }
                else
                {
                    effectsToDrag = new List<EffectPrefabData> { effect };
                }

                DragAndDrop.SetGenericData(DRAG_ID, effectsToDrag);
                // Set objectReferences to enable dropping to Hierarchy/SceneView
                DragAndDrop.objectReferences = effectsToDrag
                    .Where(e => e.Prefab != null)
                    .Select(e => e.Prefab as Object)
                    .ToArray();

                string dragTitle = effectsToDrag.Count > 1
                    ? $"{effectsToDrag.Count} effects"
                    : effect.Name;
                DragAndDrop.StartDrag(dragTitle);
                Event.current.Use();
            }

            // Right-click menu
            if (Event.current.type == EventType.ContextClick && thumbnailRect.Contains(Event.current.mousePosition))
            {
                ShowEffectContextMenu(effect);
                Event.current.Use();
            }

            // Prefab name
            EditorGUILayout.LabelField(effect.Name, thumbnailLabelStyle, GUILayout.Height(30));

            EditorGUILayout.EndVertical();

            // Draw selection highlight using EditorGUI.DrawRect (Unity 6 compatible)
            if (selectedEffects.Contains(effect))
            {
                Rect containerRect = GUILayoutUtility.GetLastRect();
                float borderWidth = 2f;
                Color highlightColor = new Color(0.2f, 0.5f, 0.8f, 1f);

                // Draw blue border (4 rectangles forming a frame)
                EditorGUI.DrawRect(new Rect(containerRect.x, containerRect.y, containerRect.width, borderWidth), highlightColor); // Top
                EditorGUI.DrawRect(new Rect(containerRect.x, containerRect.yMax - borderWidth, containerRect.width, borderWidth), highlightColor); // Bottom
                EditorGUI.DrawRect(new Rect(containerRect.x, containerRect.y, borderWidth, containerRect.height), highlightColor); // Left
                EditorGUI.DrawRect(new Rect(containerRect.xMax - borderWidth, containerRect.y, borderWidth, containerRect.height), highlightColor); // Right
            }
        }

        /// <summary>
        /// Draw empty state
        /// </summary>
        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            var centeredStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                wordWrap = true
            };

            if (isScanning)
            {
                EditorGUILayout.LabelField("Scanning prefabs...", centeredStyle);
            }
            else if (isGeneratingThumbnails)
            {
                EditorGUILayout.LabelField($"Generating thumbnails... {thumbnailGenerationIndex}/{allEffectPrefabs.Count}", centeredStyle);
            }
            else if (allEffectPrefabs.Count == 0)
            {
                EditorGUILayout.LabelField("No ParticleSystem prefabs found in project.", centeredStyle);
                EditorGUILayout.Space(20);

                // Display button centered
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Scan Now", GUILayout.Width(120), GUILayout.Height(30)))
                {
                    ScanEffectPrefabs();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else if (!string.IsNullOrEmpty(searchText))
            {
                EditorGUILayout.LabelField($"No effects matching '{searchText}'", centeredStyle);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// Refresh window after asset changes (public API for asset processors)
        /// Uses lightweight scan without thumbnail generation for better performance
        /// </summary>
        public void RefreshAfterAssetChange()
        {
            ScanEffectPrefabsWithoutThumbnailGeneration();
        }

        /// <summary>
        /// Update thumbnail for a single prefab without scanning all prefabs
        /// Called when thumbnail is captured from inspector preview
        /// </summary>
        /// <param name="guid">Prefab GUID</param>
        public void UpdateSinglePrefabThumbnail(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            // Find the effect in current list
            var effect = allEffectPrefabs.Find(e => e.Guid == guid);
            if (effect != null)
            {
                // Reload thumbnail from cache
                effect.StaticThumbnail = EffectThumbnailManager.LoadThumbnail(guid);
                Repaint();
            }
        }

        /// <summary>
        /// Scan effect prefabs
        /// </summary>
        private void ScanEffectPrefabs()
        {
            ScanEffectPrefabsInternal(startThumbnailGeneration: true);
        }

        /// <summary>
        /// Scan effect prefabs without starting thumbnail generation (for quick refresh)
        /// </summary>
        private void ScanEffectPrefabsWithoutThumbnailGeneration()
        {
            ScanEffectPrefabsInternal(startThumbnailGeneration: false);
        }

        /// <summary>
        /// Restore effect list from serialized GUIDs (lightweight, for domain reload)
        /// </summary>
        private void RestoreFromGuids()
        {
            allEffectPrefabs.Clear();
            var settings = EffectViewerSettings.Instance;

            foreach (var guid in serializedEffectGuids)
            {
                // Skip if excluded
                if (settings.IsExcluded(guid)) continue;

                var effectData = LoadEffectDataFromGuid(guid);
                if (effectData != null)
                {
                    allEffectPrefabs.Add(effectData);
                }
            }

            // Update serialized list to remove invalid entries
            serializedEffectGuids = allEffectPrefabs.Select(e => e.Guid).ToList();

            ApplyFilter();
            Repaint();
        }

        /// <summary>
        /// Add a single effect prefab to the list (for import notification)
        /// </summary>
        /// <param name="guid">Prefab GUID</param>
        public void AddEffectPrefab(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return;

            // Skip if already exists
            if (serializedEffectGuids.Contains(guid)) return;

            // Skip if excluded
            var settings = EffectViewerSettings.Instance;
            if (settings.IsExcluded(guid)) return;

            var effectData = LoadEffectDataFromGuid(guid);
            if (effectData != null)
            {
                allEffectPrefabs.Add(effectData);
                serializedEffectGuids.Add(guid);
                ApplyFilter();
                Repaint();
            }
        }

        /// <summary>
        /// Load EffectPrefabData from GUID (lightweight loading)
        /// </summary>
        private EffectPrefabData LoadEffectDataFromGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return null;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return null;

            var settings = EffectViewerSettings.Instance;
            bool isManuallyAdded = settings.IsManuallyAdded(guid);

            // Check for effect components
            var particleSystem = prefab.GetComponent<ParticleSystem>();
#if VFX_GRAPH_AVAILABLE
            var visualEffect = prefab.GetComponent<UnityEngine.VFX.VisualEffect>();
#else
            Component visualEffect = null;
#endif

            // Skip if no effect components (unless manually added)
            if (!isManuallyAdded && particleSystem == null && visualEffect == null)
            {
                return null;
            }

            var effectData = new EffectPrefabData
            {
                AssetPath = assetPath,
                Guid = guid,
                Name = prefab.name,
                Prefab = prefab,
                IsManuallyAdded = isManuallyAdded
            };

            // Determine effect type
            if (particleSystem != null)
            {
                effectData.ParticleSystemCount = 1;
                effectData.Type = EffectType.ParticleSystem;
            }
#if VFX_GRAPH_AVAILABLE
            else if (visualEffect != null)
            {
                effectData.ParticleSystemCount = 1;
                effectData.Type = EffectType.VFXGraph;
            }
#endif
            else
            {
                effectData.Type = EffectType.Unknown;
            }

            // Load thumbnail from cache
            effectData.StaticThumbnail = EffectThumbnailManager.LoadThumbnail(guid);

            return effectData;
        }

        /// <summary>
        /// Internal scan implementation
        /// </summary>
        private void ScanEffectPrefabsInternal(bool startThumbnailGeneration)
        {
            isScanning = true;
            CleanupResources();

            try
            {
                allEffectPrefabs = EffectPrefabScanner.ScanAllEffectPrefabs();

                // Save GUIDs for domain reload persistence
                serializedEffectGuids = allEffectPrefabs.Select(e => e.Guid).ToList();

                ApplyFilter();

                // After scan completion, start static thumbnail generation (if requested)
                if (startThumbnailGeneration && allEffectPrefabs.Count > 0)
                {
                    isGeneratingThumbnails = true;
                    thumbnailGenerationIndex = 0;
                }
            }
            finally
            {
                isScanning = false;
                Repaint();
            }
        }

        /// <summary>
        /// Regenerate all thumbnails (with confirmation dialog)
        /// </summary>
        private void RegenerateAllThumbnails()
        {
            if (allEffectPrefabs.Count == 0)
            {
                return;
            }

            string message = $"All existing thumbnail images will be deleted and regenerated.\n" +
                           $"This process may take some time.\n\n" +
                           $"Target effects: {allEffectPrefabs.Count}\n\n" +
                           $"Do you want to continue?";

            if (EditorUtility.DisplayDialog(
                "Regenerate All Thumbnails",
                message,
                "Regenerate",
                "Cancel"))
            {
                // Clear existing thumbnails
                foreach (var effect in allEffectPrefabs)
                {
                    effect.StaticThumbnail = null;
                }

                // Set force regenerate flag and start thumbnail generation
                forceRegenerate = true;
                isGeneratingThumbnails = true;
                thumbnailGenerationIndex = 0;
                Repaint();
            }
        }

        /// <summary>
        /// Regenerate thumbnails for specified effects
        /// </summary>
        private void RegenerateThumbnailsForEffects(List<EffectPrefabData> effects)
        {
            if (effects == null || effects.Count == 0)
                return;

            if (thumbnailGenerator == null)
            {
                thumbnailGenerator = new EffectThumbnailGenerator();
                thumbnailGenerator.Initialize();
            }

            foreach (var effect in effects)
            {
                if (effect == null || effect.Prefab == null)
                    continue;

#if VFX_GRAPH_AVAILABLE
                // VFX Graph requires multi-frame GPU warmup — use deferred capture
                if (effect.Type == EffectType.VFXGraph)
                {
                    VFXDeferredThumbnailCapture.Enqueue(effect.Guid, effect.AssetPath, effect.Prefab, THUMBNAIL_SIZE);
                    continue;
                }
#endif

                // Force regenerate thumbnail
                effect.StaticThumbnail = thumbnailGenerator.GenerateStaticThumbnail(
                    effect.Prefab,
                    THUMBNAIL_SIZE,
                    THUMBNAIL_SIZE
                );

                // Save to cache
                if (effect.StaticThumbnail != null)
                {
                    EffectThumbnailManager.SaveThumbnail(effect.Guid, effect.StaticThumbnail);
                }
            }

            Repaint();
        }


        /// <summary>
        /// Apply filter
        /// </summary>
        private void ApplyFilter()
        {
            // Invalidate cached tag counts so they are recomputed on next draw
            tagCountsDirty = true;

            // Base list (with tag filter applied)
            List<EffectPrefabData> baseList;

            if (string.IsNullOrEmpty(selectedTag))
            {
                // No tag selected = show all
                baseList = allEffectPrefabs;
            }
            else
            {
                // Filter by selected tag
                baseList = EffectTagManager.FilterByTag(allEffectPrefabs, selectedTag);
            }

            // Further filter by search text
            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredEffectPrefabs = new List<EffectPrefabData>(baseList);
            }
            else
            {
                filteredEffectPrefabs = EffectPrefabScanner.FilterByName(baseList, searchText);
            }

            Repaint();
        }

        // ──────────────────────────────────────────────
        // Cached icon helpers (loaded once, reused every frame)
        // ──────────────────────────────────────────────

        private static Texture2D GetCachedEffectIcon(EffectType type)
        {
            EnsureIconsCached();
            return type == EffectType.ParticleSystem ? s_ParticleSystemIcon : s_VFXGraphIcon;
        }

        private static Texture2D GetCachedManualAddIcon()
        {
            EnsureIconsCached();
            return s_ManualAddIcon;
        }

        private static void EnsureIconsCached()
        {
            if (s_IconsCached) return;

            s_ParticleSystemIcon = LoadIcon("d_ParticleSystem Icon", "ParticleSystem Icon");
            s_VFXGraphIcon = LoadIcon("d_VisualEffect Icon", "VisualEffect Icon", "d_VisualEffect Gizmo", "VisualEffect Gizmo");
            s_ManualAddIcon = LoadIcon("d_Grid.MoveTool", "d_Toolbar Plus");

            // Only cache once all icons are successfully loaded
            if (s_ParticleSystemIcon != null && s_VFXGraphIcon != null && s_ManualAddIcon != null)
                s_IconsCached = true;
        }

        private static Texture2D LoadIcon(params string[] names)
        {
            foreach (var name in names)
            {
                var content = EditorGUIUtility.IconContent(name);
                if (content?.image != null)
                    return content.image as Texture2D;
            }
            return null;
        }

        /// <summary>
        /// Remove prefab from viewer (unified for manual and auto-detected)
        /// </summary>
        private void RemoveFromViewer(EffectPrefabData effect)
        {
            RemoveFromViewer(new List<EffectPrefabData> { effect });
        }

        /// <summary>
        /// Remove multiple prefabs from viewer (supports multi-selection)
        /// </summary>
        private void RemoveFromViewer(List<EffectPrefabData> effects)
        {
            if (effects == null || effects.Count == 0) return;

            string message;
            if (effects.Count == 1)
            {
                var effect = effects[0];
                message = effect.IsManuallyAdded
                    ? $"Remove '{effect.Name}' from Effect Viewer?\n\nThis will remove it from the manually added list."
                    : $"Remove '{effect.Name}' from Effect Viewer?\n\nThis prefab will no longer appear in the viewer.";
            }
            else
            {
                message = $"Remove {effects.Count} items from Effect Viewer?\n\nThese prefabs will no longer appear in the viewer.";
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Remove from Viewer",
                message,
                "Remove",
                "Cancel"
            );

            if (confirmed)
            {
                var settings = EffectViewerSettings.Instance;

                foreach (var effect in effects)
                {
                    if (effect.IsManuallyAdded)
                    {
                        // Remove from manual list
                        settings.RemoveManualPrefab(effect.Guid);
                    }
                    else
                    {
                        // Add to exclusion list (for auto-detected)
                        settings.ExcludePrefab(effect.Guid);
                    }
                }

                selectedEffects.Clear();
                ScanEffectPrefabs();
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        private void CleanupResources()
        {
            foreach (var effect in allEffectPrefabs)
            {
                effect.Cleanup();
            }
        }

        /// <summary>
        /// Draw status bar (selected prefab path)
        /// </summary>
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(STATUSBAR_HEIGHT));

            if (selectedEffects.Count == 1)
            {
                var effect = selectedEffects.First();
                if (effect.Prefab != null)
                {
                    string path = AssetDatabase.GetAssetPath(effect.Prefab);
                    EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("", EditorStyles.miniLabel);
                }
            }
            else if (selectedEffects.Count > 1)
            {
                EditorGUILayout.LabelField($"{selectedEffects.Count} items selected", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Opens the specified effect prefab in Prefab Mode
        /// </summary>
        private void OpenPrefabMode(EffectPrefabData effect)
        {
            if (effect == null) return;

            if (effect.Prefab != null)
            {
                AssetDatabase.OpenAsset(effect.Prefab);
                return;
            }

            if (!string.IsNullOrEmpty(effect.AssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(effect.AssetPath);
                if (prefab != null)
                {
                    AssetDatabase.OpenAsset(prefab);
                }
            }
        }
    }
}
