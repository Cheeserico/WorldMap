using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CountryPieceGenerator : EditorWindow
{
    private TextAsset countryDataCsv;

    private RectTransform pieceContent;
    private RectTransform dragLayer;

    private GameObject countryPiecePrefab;

    [MenuItem("Tools/World Map/Generate Country Pieces")]
    public static void ShowWindow()
    {
        GetWindow<CountryPieceGenerator>(
            "Country Piece Generator"
        );
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "Country Piece Generator",
            EditorStyles.boldLabel
        );

        countryDataCsv =
            (TextAsset)EditorGUILayout.ObjectField(
                "CountryData CSV",
                countryDataCsv,
                typeof(TextAsset),
                false
            );

        pieceContent =
            (RectTransform)EditorGUILayout.ObjectField(
                "Piece Content",
                pieceContent,
                typeof(RectTransform),
                true
            );

        dragLayer =
            (RectTransform)EditorGUILayout.ObjectField(
                "Drag Layer",
                dragLayer,
                typeof(RectTransform),
                true
            );

        countryPiecePrefab =
            (GameObject)EditorGUILayout.ObjectField(
                "Country Piece Prefab",
                countryPiecePrefab,
                typeof(GameObject),
                false
            );

        EditorGUILayout.Space();

        if (GUILayout.Button(
            "Generate Country Pieces"
        ))
        {
            GeneratePieces();
        }
    }

    private void GeneratePieces()
    {
        if (countryDataCsv == null)
        {
            Debug.LogError(
                "CountryData.csv を設定してください。"
            );
            return;
        }

        if (pieceContent == null)
        {
            Debug.LogError(
                "PieceContent を設定してください。"
            );
            return;
        }

        if (dragLayer == null)
        {
            Debug.LogError(
                "DragLayer を設定してください。"
            );
            return;
        }

        if (countryPiecePrefab == null)
        {
            Debug.LogError(
                "CountryPiecePrefab を設定してください。"
            );
            return;
        }

        string[] lines =
            countryDataCsv.text.Split(
                new[] { "\r\n", "\n" },
                System.StringSplitOptions.RemoveEmptyEntries
            );

        int generatedCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');

            if (values.Length < 8)
            {
                Debug.LogWarning(
                    "CSV形式がおかしい行をスキップ: "
                    + lines[i]
                );
                continue;
            }

            string id = values[0];
            string adminName = values[1];

            float sourceWidth =
                ParseFloat(values[4]);

            float sourceHeight =
                ParseFloat(values[5]);

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
                    "Spriteが見つかりません: "
                    + id
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
                    "Sprite読み込み失敗: "
                    + id
                );
                continue;
            }

            // =========================
            // 古い同名Piece削除
            // =========================

            Transform oldPiece =
                pieceContent.Find(
                    id + "_CountryPiece"
                );

            if (oldPiece != null)
            {
                Undo.DestroyObjectImmediate(
                    oldPiece.gameObject
                );
            }

            // =========================
            // Prefab生成
            // =========================

            GameObject pieceObject =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    countryPiecePrefab,
                    pieceContent
                );

            if (pieceObject == null)
            {
                Debug.LogError(
                    "Prefab生成に失敗しました: "
                    + id
                );
                continue;
            }

            Undo.RegisterCreatedObjectUndo(
                pieceObject,
                "Generate Country Piece"
            );

            pieceObject.name =
                id + "_CountryPiece";

            // =========================
            // CountryPieceDrag
            // =========================

            CountryPieceDrag pieceDrag =
                pieceObject.GetComponent<CountryPieceDrag>();

            if (pieceDrag == null)
            {
                Debug.LogError(
                    "PrefabにCountryPieceDragがありません: "
                    + id
                );

                Undo.DestroyObjectImmediate(
                    pieceObject
                );

                continue;
            }

            SerializedObject serializedDrag =
                new SerializedObject(
                    pieceDrag
                );

            SerializedProperty countryIdProperty =
                serializedDrag.FindProperty(
                    "countryId"
                );

            SerializedProperty dragLayerProperty =
                serializedDrag.FindProperty(
                    "dragLayer"
                );

            if (countryIdProperty != null)
            {
                countryIdProperty.stringValue =
                    id;
            }

            if (dragLayerProperty != null)
            {
                dragLayerProperty.objectReferenceValue =
                    dragLayer;
            }

            serializedDrag.ApplyModifiedProperties();

            // =========================
            // Country_Imageを探す
            // =========================

            Transform imageTransform =
                FindChildRecursive(
                    pieceObject.transform,
                    "Country_Image"
                );

            if (imageTransform == null)
            {
                Debug.LogWarning(
                    "Country_Imageが見つかりません: "
                    + id
                );
            }
            else
            {
                Image countryImage =
                    imageTransform.GetComponent<Image>();

                if (countryImage != null)
                {
                    countryImage.sprite = sprite;
                    countryImage.color =
    
                        new Color32(
                            65,
                            65,
                            65,
                            255
                            );
                    countryImage.type =
                        Image.Type.Simple;
                    countryImage.preserveAspect =
                        true;
                }
            }

            // =========================
            // Country_Textを探す
            // =========================

            Transform textTransform =
                FindChildRecursive(
                    pieceObject.transform,
                    "Country_Text"
                );

            if (textTransform != null)
            {
                TMP_Text countryText =
                    textTransform.GetComponent<TMP_Text>();

                if (countryText != null)
                {
                    countryText.text =
                        adminName;
                }
            }

            // =========================
            // 見た目サイズ調整
            // =========================

            RectTransform pieceRect =
                pieceObject.GetComponent<RectTransform>();

            if (pieceRect != null)
            {
                // ScrollViewでは基本130x130の枠に統一
                pieceRect.sizeDelta =
                    new Vector2(
                        130f,
                        130f
                    );
            }

            // =========================
            // Country_Imageの大きさ調整
            // =========================

            if (imageTransform != null)
            {
                RectTransform imageRect =
                    imageTransform as RectTransform;

                if (imageRect != null)
                {
                    float maxDisplaySize = 100f;

                    float scale =
                        Mathf.Min(
                            maxDisplaySize / sourceWidth,
                            maxDisplaySize / sourceHeight
                        );

                    // 超小国が小さすぎないよう最低サイズ確保
                    float displayWidth =
                        sourceWidth * scale;

                    float displayHeight =
                        sourceHeight * scale;

                    float minDisplaySize = 35f;

                    if (
                        displayWidth < minDisplaySize &&
                        displayHeight < minDisplaySize
                    )
                    {
                        float upScale =
                            minDisplaySize /
                            Mathf.Max(
                                displayWidth,
                                displayHeight
                            );

                        displayWidth *= upScale;
                        displayHeight *= upScale;
                    }

                    imageRect.sizeDelta =
                        new Vector2(
                            displayWidth,
                            displayHeight
                        );
                }
            }

            generatedCount++;

            Debug.Log(
                "生成: "
                + id
                + " / "
                + adminName
            );
        }

        EditorUtility.SetDirty(
            pieceContent
        );

        EditorSceneManager.MarkSceneDirty(
            pieceContent.gameObject.scene
        );

        Debug.Log(
            "CountryPiece生成完了: "
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

    private Transform FindChildRecursive(
        Transform parent,
        string childName
    )
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform result =
                FindChildRecursive(
                    child,
                    childName
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}