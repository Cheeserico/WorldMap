using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class CountrySlotGenerator : EditorWindow
{
    private TextAsset countryDataCsv;

    private RectTransform countrySlotsRoot;
    private RectTransform placedCountryRoot;
    private CountryPuzzleManager puzzleManager;

    private AnswerFeedbackUI answerFeedbackUI;
    private CountryFlagDatabase countryFlagDatabase;

    private const float SourceMapWidth = 4096f;
    private const float SourceMapHeight = 2048f;

    // Drop判定の大きさ
    // private const float DropTargetSize = 32f;

    [MenuItem("Tools/World Map/Generate Country Slots")]
    public static void ShowWindow()
    {
        GetWindow<CountrySlotGenerator>(
            "Country Slot Generator"
        );
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "Country Slot Generator",
            EditorStyles.boldLabel
        );

        countryDataCsv =
            (TextAsset)EditorGUILayout.ObjectField(
                "CountryData CSV",
                countryDataCsv,
                typeof(TextAsset),
                false
            );

        countrySlotsRoot =
            (RectTransform)EditorGUILayout.ObjectField(
                "CountrySlots Root",
                countrySlotsRoot,
                typeof(RectTransform),
                true
            );

        placedCountryRoot =
            (RectTransform)EditorGUILayout.ObjectField(
                "PlacedCountryRoot",
                placedCountryRoot,
                typeof(RectTransform),
                true
            );

        puzzleManager =
            (CountryPuzzleManager)EditorGUILayout.ObjectField(
                "Puzzle Manager",
                puzzleManager,
                typeof(CountryPuzzleManager),
                true
            );

        answerFeedbackUI =
    (AnswerFeedbackUI)EditorGUILayout.ObjectField(
        "Answer Feedback UI",
        answerFeedbackUI,
        typeof(AnswerFeedbackUI),
        true
    );

        countryFlagDatabase =
            (CountryFlagDatabase)EditorGUILayout.ObjectField(
                "Country Flag Database",
                countryFlagDatabase,
                typeof(CountryFlagDatabase),
                true
            );


        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Country Slots"))
        {
            GenerateSlots();
        }
    }

    private void GenerateSlots()
    {
        // =========================
        // 必須チェック
        // =========================

        if (countryDataCsv == null)
        {
            Debug.LogError(
                "CountryData_WithDropPoint.csv を設定してください。"
            );
            return;
        }

        if (countrySlotsRoot == null)
        {
            Debug.LogError(
                "CountrySlots Root を設定してください。"
            );
            return;
        }

        if (placedCountryRoot == null)
        {
            Debug.LogError(
                "PlacedCountryRoot を設定してください。"
            );
            return;
        }

        if (puzzleManager == null)
        {
            Debug.LogError(
                "Puzzle Manager を設定してください。"
            );
            return;
        }

        RectTransform mapContent =
            countrySlotsRoot.parent as RectTransform;

        if (mapContent == null)
        {
            Debug.LogError(
                "CountrySlots の親にRectTransformがありません。"
            );
            return;
        }

        float mapWidth =
            mapContent.rect.width;

        float mapHeight =
            mapContent.rect.height;

        float scaleX =
            mapWidth / SourceMapWidth;

        float scaleY =
            mapHeight / SourceMapHeight;

        // =========================
        // CSV読み込み
        // =========================

        string[] lines =
            countryDataCsv.text.Split(
                new[] { "\r\n", "\n" },
                System.StringSplitOptions.RemoveEmptyEntries
            );

        int generatedCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values =
                lines[i].Split(',');

            // 今回は10列必要
            if (values.Length < 10)
            {
                Debug.LogWarning(
                    "drop_x / drop_y がない行をスキップ: "
                    + lines[i]
                );

                continue;
            }

            string id =
                values[0];

            string adminName =
                values[1];

            float width =
                ParseFloat(values[4]);

            float height =
                ParseFloat(values[5]);

            float centerX =
                ParseFloat(values[6]);

            float centerY =
                ParseFloat(values[7]);

            float dropX =
                ParseFloat(values[8]);

            float dropY =
                ParseFloat(values[9]);

            // =========================
            // Sprite検索
            // =========================

            string[] guids =
                AssetDatabase.FindAssets(
                    id + " t:Sprite",
                    new[]
                    {
                        "Assets/A_Cheese/Maps/CountryPieces"
                    }
                );

            if (guids.Length == 0)
            {
                Debug.LogWarning(
                    "Spriteが見つかりません: " + id
                );
                continue;
            }

            string spritePath =
                AssetDatabase.GUIDToAssetPath(
                    guids[0]
                );

            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    spritePath
                );

            if (sprite == null)
            {
                Debug.LogWarning(
                    "Sprite読み込み失敗: " + id
                );
                continue;
            }

            // =========================
            // 既存Slot削除
            // =========================

            Transform oldSlot =
                countrySlotsRoot.Find(
                    id + "_Slot"
                );

            if (oldSlot != null)
            {
                Undo.DestroyObjectImmediate(
                    oldSlot.gameObject
                );
            }

            // =========================
            // Slot本体生成
            // =========================

            GameObject slotObject =
                new GameObject(
                    id + "_Slot",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CountrySlot)
                );

            Undo.RegisterCreatedObjectUndo(
                slotObject,
                "Generate Country Slot"
            );

            RectTransform slotRect =
                slotObject.GetComponent<RectTransform>();

            slotRect.SetParent(
                countrySlotsRoot,
                false
            );

            slotRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            slotRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            slotRect.pivot =
                new Vector2(0.5f, 0.5f);

            // 国画像の本来サイズ
            slotRect.sizeDelta =
                new Vector2(
                    width * scaleX,
                    height * scaleY
                );

            // 国画像の中心位置
            float unityX =
                centerX * scaleX
                - mapWidth / 2f;

            float unityY =
                mapHeight / 2f
                - centerY * scaleY;

            slotRect.anchoredPosition =
                new Vector2(
                    unityX,
                    unityY
                );

            // =========================
            // 国画像
            // =========================

            Image slotImage =
                slotObject.GetComponent<Image>();

            slotImage.sprite =
                sprite;

            slotImage.type =
                Image.Type.Simple;

            slotImage.preserveAspect =
                false;

            // 今は確認用に黄緑
            slotImage.color =
                new Color32(
                    170,
                    205,
                    70,
                    255
                );

            // ★重要
            // 国画像そのものでは判定しない
            slotImage.raycastTarget =
                false;

            // =========================
            // CountrySlot設定
            // =========================

            CountrySlot countrySlot =
                slotObject.GetComponent<CountrySlot>();

            SerializedObject serializedSlot =
                new SerializedObject(
                    countrySlot
                );

            serializedSlot
                .FindProperty("countryId")
                .stringValue =
                id;

            serializedSlot
                .FindProperty("placedCountryRoot")
                .objectReferenceValue =
                placedCountryRoot;

            serializedSlot
                .FindProperty("slotImage")
                .objectReferenceValue =
                slotImage;

            serializedSlot
                .FindProperty("puzzleManager")
                .objectReferenceValue =
                puzzleManager;

            serializedSlot
                .FindProperty("answerFeedbackUI")
                .objectReferenceValue =
                answerFeedbackUI;

            serializedSlot
                .FindProperty("countryFlagDatabase")
                .objectReferenceValue =
                countryFlagDatabase;

            serializedSlot.ApplyModifiedProperties();

            // =========================
            // DropTarget生成
            // =========================

            GameObject dropObject =
                new GameObject(
                    "DropTarget",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CountryDropTarget)
                );

            RectTransform dropRect =
                dropObject.GetComponent<RectTransform>();

            dropRect.SetParent(
                slotRect,
                false
            );

            dropRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            dropRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            dropRect.pivot =
                new Vector2(0.5f, 0.5f);

            // =========================
            // DropTargetの大きさ
            // =========================

            // 国本来の表示サイズを基準に1.5倍
            float dropWidth =
                width * scaleX * 1.5f;

            float dropHeight =
                height * scaleY * 1.5f;

            // 小さい国でも操作しやすいよう
            // 最低80x80を確保
            float minimumDropSize = 80f;

            dropWidth =
                Mathf.Max(
                    dropWidth,
                    minimumDropSize
                );

            dropHeight =
                Mathf.Max(
                    dropHeight,
                    minimumDropSize
                );

            dropRect.sizeDelta =
                new Vector2(
                    dropWidth,
                    dropHeight
                );            // =========================
            // drop_x / drop_yを
            // Unity座標へ変換
            // =========================

            float dropUnityX =
                dropX * scaleX
                - mapWidth / 2f;

            float dropUnityY =
                mapHeight / 2f
                - dropY * scaleY;

            // DropTargetはSlotの子なので
            // Slot中心との差を使う
            dropRect.anchoredPosition =
                new Vector2(
                    dropUnityX - unityX,
                    dropUnityY - unityY
                );

            // =========================
            // DropTarget Image
            // =========================

            Image dropImage =
                dropObject.GetComponent<Image>();

            // 完全透明でもRaycast可能
            dropImage.color =
                new Color(
                    1f,
                    1f,
                    0f,
                    0f
                );

            dropImage.raycastTarget =
                true;

            // =========================
            // CountryDropTarget設定
            // =========================

            CountryDropTarget dropTarget =
                dropObject.GetComponent<CountryDropTarget>();

            SerializedObject serializedDrop =
                new SerializedObject(
                    dropTarget
                );

            serializedDrop
                .FindProperty("countrySlot")
                .objectReferenceValue =
                countrySlot;

            serializedDrop.ApplyModifiedProperties();

            generatedCount++;

            Debug.Log(
                "生成: "
                + id
                + " / "
                + adminName
            );
        }

        EditorUtility.SetDirty(
            countrySlotsRoot
        );

        EditorSceneManager.MarkSceneDirty(
            countrySlotsRoot.gameObject.scene
        );

        Debug.Log(
            "CountrySlot生成完了: "
            + generatedCount
            + " 個"
        );
    }

    private float ParseFloat(
        string value
    )
    {
        return float.Parse(
            value,
            CultureInfo.InvariantCulture
        );
    }
}