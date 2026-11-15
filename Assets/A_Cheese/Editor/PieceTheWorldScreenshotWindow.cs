#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Manual screenshot capture for Piece the World. Editor only.</summary>
public class PieceTheWorldScreenshotWindow : EditorWindow
{
    private static readonly string[] PresetNames =
    {
        "1920 x 1080", "2208 x 1242", "2688 x 1242",
        "2732 x 2048 (iPad)", "2796 x 1290", "2556 x 1179 (iPhone)", "Custom"
    };
    private static readonly Vector2Int[] Presets =
    {
        new Vector2Int(1920, 1080), new Vector2Int(2208, 1242),
        new Vector2Int(2688, 1242), new Vector2Int(2732, 2048),
        new Vector2Int(2796, 1290), new Vector2Int(2556, 1179)
    };

    private string folder = "Screenshots/PieceTheWorld";
    private string language = "en";
    private string sceneLabel = "Gameplay";
    private int presetIndex;
    private int customWidth = 1920;
    private int customHeight = 1080;
    private string status = "Ready";
    private bool capturing;
    private double captureAt;
    private double deadline;
    private string pendingPath;
    private int previousSizeIndex = -1;
    private EditorWindow gameView;
    private bool sizeChanged;
    private bool screenshotRequested;
    private Vector2Int requestedResolution;
    private int matchingFrames;
    private bool captureAll;
    private Vector2Int[] batchResolutions;
    private string batchLabel;
    private int batchIndex;
    private int completedCount;
    private string outputDirectory;
    private string safeLang;
    private string safeScene;
    private PropertyInfo selectedSizeProperty;
    private double fileStableSince;
    private long lastFileLength = -1;


    [MenuItem("Tools/Piece the World/Screenshot Capture")]
    private static void Open() => GetWindow<PieceTheWorldScreenshotWindow>("PTW Screenshots");

    private void OnEnable()
    {
        folder = EditorPrefs.GetString("PTW_Screenshot_Folder", folder);
        language = EditorPrefs.GetString("PTW_Screenshot_Language", language);
        sceneLabel = EditorPrefs.GetString("PTW_Screenshot_Scene", sceneLabel);
        EditorApplication.update += UpdateCapture;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateCapture;
        if (capturing) Finish("Capture cancelled; Game view setting restored.");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Piece the World | Screenshot Capture", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter Play mode, show the scene you want, then capture. This tool changes Game view resolution temporarily.", MessageType.Info);
        using (new EditorGUI.DisabledScope(capturing))
        {
            presetIndex = EditorGUILayout.Popup("Resolution", presetIndex, PresetNames);
            if (presetIndex == Presets.Length)
            {
                customWidth = EditorGUILayout.IntField("Width", customWidth);
                customHeight = EditorGUILayout.IntField("Height", customHeight);
            }
            language = EditorGUILayout.TextField("Language code", language);
            sceneLabel = EditorGUILayout.TextField("Screen name", sceneLabel);
            folder = EditorGUILayout.TextField("Output folder", folder);
            if (GUILayout.Button("Choose folder..."))
            {
                string selected = EditorUtility.OpenFolderPanel("Screenshot output folder", folder, "");
                if (!string.IsNullOrEmpty(selected)) folder = selected;
            }
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Capture PNG", GUILayout.Height(36))) BeginCapture(false);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Store presets", EditorStyles.boldLabel);
                if (GUILayout.Button("Google Play (1920 x 1080)", GUILayout.Height(30)))
                    BeginBatch("Google Play", new Vector2Int(1920, 1080));
                if (GUILayout.Button("App Store (3 sizes)", GUILayout.Height(30)))
                    BeginBatch("App Store", new Vector2Int(2796, 1290),
                        new Vector2Int(2556, 1179), new Vector2Int(2732, 2048));
                if (GUILayout.Button("Capture All 6 Sizes", GUILayout.Height(30)))
                    BeginBatch("All sizes", Presets);
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(status, MessageType.None);
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play mode to enable capture.", MessageType.Warning);
    }

    private void BeginBatch(string label, params Vector2Int[] resolutions)
    {
        BeginCapture(true, resolutions, label);
    }

    private void BeginCapture(bool allSizes, Vector2Int[] resolutions = null, string label = null)
    {
        if (capturing || !Application.isPlaying) return;
        Vector2Int resolution = presetIndex < Presets.Length
            ? Presets[presetIndex] : new Vector2Int(customWidth, customHeight);
        if (!allSizes && (resolution.x < 1 || resolution.y < 1 || resolution.x > 8192 || resolution.y > 8192))
        {
            status = "Invalid resolution (allowed: 1..8192 per dimension).";
            return;
        }
        if (string.IsNullOrWhiteSpace(folder)) { status = "Select an output folder."; return; }

        try
        {
            outputDirectory = Path.IsPathRooted(folder) ? folder : Path.Combine(Directory.GetCurrentDirectory(), folder);
            Directory.CreateDirectory(outputDirectory);
            safeLang = SafeName(language, "en");
            safeScene = SafeName(sceneLabel, "Gameplay");
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) throw new Exception("GameView type was not found.");
            gameView = EditorWindow.GetWindow(gameViewType);
            if (gameView == null) throw new Exception("Cannot open Game view.");
            selectedSizeProperty = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (selectedSizeProperty == null) throw new Exception("Game view size selector is unavailable.");
            previousSizeIndex = (int)selectedSizeProperty.GetValue(gameView);
            EditorPrefs.SetString("PTW_Screenshot_Folder", folder);
            EditorPrefs.SetString("PTW_Screenshot_Language", language);
            EditorPrefs.SetString("PTW_Screenshot_Scene", sceneLabel);
            capturing = true;
            captureAll = allSizes;
            batchResolutions = allSizes ? resolutions : new[] { resolution };
            batchLabel = allSizes ? label : "Single capture";
            if (batchResolutions == null || batchResolutions.Length == 0)
                throw new Exception("No resolutions selected.");
            batchIndex = 0;
            completedCount = 0;
            StartResolution(batchResolutions[0]);
        }
        catch (Exception ex)
        {
            Finish("Cannot start capture: " + ex.Message);
        }
    }

    private void StartResolution(Vector2Int resolution)
    {
        requestedResolution = resolution;
        string name = string.Format("PTW_{0}_{1}_{2}x{3}_{4}_{5}.png", safeLang, safeScene,
            resolution.x, resolution.y, DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"),
            Guid.NewGuid().ToString("N").Substring(0, 6));
        pendingPath = Path.Combine(outputDirectory, name);
        int newIndex = AddTemporaryResolution(resolution.x, resolution.y);
        selectedSizeProperty.SetValue(gameView, newIndex);
        sizeChanged = true;
        gameView.Focus();
        gameView.Repaint();
        screenshotRequested = false;
        matchingFrames = 0;
        lastFileLength = -1;
        fileStableSince = 0;
        captureAt = EditorApplication.timeSinceStartup + 0.8;
        deadline = EditorApplication.timeSinceStartup + 15;
        status = (captureAll ? "Capture " + (batchIndex + 1) + "/" + batchResolutions.Length + ": " : "Capture: ") +
                 "Preparing " + resolution.x + "x" + resolution.y + "...";
        Repaint();
    }

    private static int AddTemporaryResolution(int width, int height)
    {
        Assembly editorAssembly = typeof(EditorWindow).Assembly;
        Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
        Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
        Type sizeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
        if (sizesType == null || sizeType == null || sizeEnum == null)
            throw new Exception("Unity Game view resolution API is unavailable.");

        Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        object sizes = singleton.GetProperty("instance").GetValue(null);
        MethodInfo getGroup = sizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (getGroup == null) throw new Exception("Game view resolution group API is unavailable.");

        // Use the active build target's resolution group rather than always Standalone.
        GameViewSizeGroupType groupType = GameViewSizeGroupType.Standalone;
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.Android:
                groupType = GameViewSizeGroupType.Android;
                break;
            case BuildTarget.iOS:
                groupType = GameViewSizeGroupType.iOS;
                break;
        }
        object group = getGroup.Invoke(sizes, new object[] { (int)groupType });
        if (group == null) throw new Exception("Game view resolution group was not found.");
        MethodInfo countMethod = group.GetType().GetMethod("GetTotalCount");
        MethodInfo addMethod = group.GetType().GetMethod("AddCustomSize");
        if (countMethod == null || addMethod == null)
            throw new Exception("Game view resolution list API is unavailable.");
        int before = (int)countMethod.Invoke(group, null);
        object fixedResolution = Enum.Parse(sizeEnum, "FixedResolution");
        object size = Activator.CreateInstance(sizeType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { fixedResolution, width, height, "PTW Temporary " + width + "x" + height }, null);
        addMethod.Invoke(group, new[] { size });
        return before;
    }

    private void UpdateCapture()
    {
        if (!capturing) return;
        double now = EditorApplication.timeSinceStartup;
        if (!Application.isPlaying) { Finish("Capture cancelled: Play mode ended."); return; }
        if (now > deadline)
        {
            Finish("Capture stopped after " + completedCount + " file(s): expected " +
                requestedResolution.x + "x" + requestedResolution.y +
                ", actual " + Screen.width + "x" + Screen.height + ".");
            return;
        }
        if (!screenshotRequested)
        {
            if (Screen.width == requestedResolution.x && Screen.height == requestedResolution.y)
                matchingFrames++;
            else
                matchingFrames = 0;

            if (now >= captureAt && matchingFrames >= 3)
            {
                try
                {
                    ScreenCapture.CaptureScreenshot(pendingPath);
                    screenshotRequested = true;
                    status = "Saving " + requestedResolution.x + "x" + requestedResolution.y + "...";
                }
                catch (Exception ex) { Finish("Capture failed: " + ex.Message); }
            }
            else
            {
                status = "Waiting for " + requestedResolution.x + "x" + requestedResolution.y +
                    " (actual " + Screen.width + "x" + Screen.height + ")...";
            }
            Repaint();
        }
        else if (File.Exists(pendingPath))
        {
            try
            {
                long length = new FileInfo(pendingPath).Length;
                if (length <= 24) return;
                if (length != lastFileLength)
                {
                    lastFileLength = length;
                    fileStableSince = now;
                    return;
                }
                if (now - fileStableSince < 0.35) return;
                if (!PngMatchesSize(pendingPath, requestedResolution))
                {
                    Finish("PNG dimensions do not match " + requestedResolution.x + "x" +
                        requestedResolution.y + ". Check: " + pendingPath);
                    return;
                }
                completedCount++;
                if (captureAll && batchIndex + 1 < batchResolutions.Length)
                {
                    batchIndex++;
                    try { StartResolution(batchResolutions[batchIndex]); }
                    catch (Exception ex) { Finish("Stopped after " + completedCount + " file(s): " + ex.Message); }
                }
                else
                {
                    Finish(captureAll ? "Saved " + completedCount + " PNGs (" + batchLabel + ") to: " + outputDirectory
                        : "Saved: " + pendingPath);
                }
            }
            catch (IOException) { /* Wait for screenshot writer to finish. */ }
        }
    }

    // PNG IHDR stores width/height in big-endian bytes at offsets 16 and 20.
    private static bool PngMatchesSize(string path, Vector2Int expected)
    {
        using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            byte[] header = reader.ReadBytes(24);
            if (header.Length < 24 || header[0] != 137 || header[1] != 80 ||
                header[2] != 78 || header[3] != 71) return false;
            int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return width == expected.x && height == expected.y;
        }
    }

    private void Finish(string message)
    {
        try
        {
            if (sizeChanged && gameView != null && previousSizeIndex >= 0)
            {
                PropertyInfo selectedSize = gameView.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                selectedSize?.SetValue(gameView, previousSizeIndex);
            }
        }
        catch (Exception ex) { message += " (Could not restore Game view size: " + ex.Message + ")"; }
        sizeChanged = false;
        captureAll = false;
        batchResolutions = null;
        capturing = false;
        screenshotRequested = false;
        status = message;
        Repaint();
    }

    private static string SafeName(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        value = value.Replace('/', '_').Replace('\\', '_').Trim();
        return value.Length == 0 ? fallback : value;
    }
}
#endif
