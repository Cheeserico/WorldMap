using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// StageDataを作成するためのEditorWindow。
/// </summary>
public class StageDataCreatorWindow : EditorWindow
{

    // ==================================================
    // 入力データ
    // ==================================================

    private string stageId = "stage_001";
    private string stageName = "東アジア";

    private Sprite stageImage;

    private float threeStarTime = 30f;
    private float twoStarTime = 60f;

    private Vector2 mapStartPosition =
        Vector2.zero;

    private float mapStartScale = 1f;


    // ==================================================
    // CSV
    // ==================================================

    [System.Serializable]
    private class CountryEntry
    {
        public string countryId;
        public string countryName;

        public bool selected;
    }

    private TextAsset countryCsv;

    private readonly List<CountryEntry> countries =
        new List<CountryEntry>();

    private Vector2 countryScrollPosition;
    private Vector2 mainScrollPosition;


    // ==================================================
    // Windowを開く
    // ==================================================

    [MenuItem("World Map Puzzle/Stage Data Creator")]
    public static void OpenWindow()
    {
        StageDataCreatorWindow window =
            GetWindow<StageDataCreatorWindow>();

        window.titleContent =
            new GUIContent("Stage Data Creator");

        window.minSize =
            new Vector2(450f, 600f);
    }


    // ==================================================
    // Window表示
    // ==================================================

    private void OnGUI()
    {
        mainScrollPosition =
            EditorGUILayout.BeginScrollView(
                mainScrollPosition
            );

        GUILayout.Space(10f);

        EditorGUILayout.LabelField(
            "Stage Data Creator",
            EditorStyles.boldLabel
        );


        // ==================================================
        // 基本情報
        // ==================================================

        EditorGUILayout.LabelField(
            "ステージ基本情報",
            EditorStyles.boldLabel
        );

        stageId =
            EditorGUILayout.TextField(
                "Stage ID",
                stageId
            );

        stageName =
            EditorGUILayout.TextField(
                "Stage Name",
                stageName
            );

        stageImage =
            (Sprite)EditorGUILayout.ObjectField(
                "Stage Image",
                stageImage,
                typeof(Sprite),
                false
            );


        GUILayout.Space(15f);


        // ==================================================
        // 星評価
        // ==================================================

        EditorGUILayout.LabelField(
            "星評価タイム",
            EditorStyles.boldLabel
        );

        threeStarTime =
            EditorGUILayout.FloatField(
                "Three Star Time",
                threeStarTime
            );

        twoStarTime =
            EditorGUILayout.FloatField(
                "Two Star Time",
                twoStarTime
            );


        GUILayout.Space(15f);


        // ==================================================
        // 地図開始位置
        // ==================================================

        EditorGUILayout.LabelField(
            "開始時の地図表示",
            EditorStyles.boldLabel
        );

        mapStartPosition =
            EditorGUILayout.Vector2Field(
                "Map Start Position",
                mapStartPosition
            );

        mapStartScale =
            EditorGUILayout.FloatField(
                "Map Start Scale",
                mapStartScale
            );


        GUILayout.Space(20f);


        // ==================================================
        // CountryData.csv
        // ==================================================

        EditorGUILayout.LabelField(
            "国データ",
            EditorStyles.boldLabel
        );

        TextAsset newCsv =
            (TextAsset)EditorGUILayout.ObjectField(
                "Country CSV",
                countryCsv,
                typeof(TextAsset),
                false
            );

        if (newCsv != countryCsv)
        {
            countryCsv = newCsv;

            LoadCountriesFromCsv();
        }


        GUILayout.Space(10f);

        GUILayout.Space(15f);

        // ==================================================
        // 地域プリセット
        // ==================================================

        EditorGUILayout.LabelField(
            "地域プリセット",
            EditorStyles.boldLabel
        );

        GUILayout.Space(5f);


        // ==================================================
        // アジア
        // ==================================================

        EditorGUILayout.LabelField(
            "アジア",
            EditorStyles.miniBoldLabel
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("01 東アジア"))
        {
            SelectPreset(
                new string[]
                {
            "CHN",
            "JPN",
            "MNG",
            "PRK",
            "KOR"
                }
            );
        }

        if (GUILayout.Button("02 東南アジア"))
        {
            SelectPreset(
                new string[]
                {
            "BRN",
            "KHM",
            "IDN",
            "LAO",
            "MYS",
            "MMR",
            "PHL",
            "SGP",
            "THA",
            "TLS",
            "VNM"
                }
            );
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("03 南アジア"))
        {
            SelectPreset(
                new string[]
                {
            "AFG",
            "BGD",
            "BTN",
            "IND",
            "MDV",
            "NPL",
            "PAK",
            "LKA"
                }
            );
        }

        if (GUILayout.Button("04 中央アジア"))
        {
            SelectPreset(
                new string[]
                {
            "KAZ",
            "KGZ",
            "TJK",
            "TKM",
            "UZB"
                }
            );
        }

        if (GUILayout.Button("05 中東"))
        {
            SelectPreset(
                new string[]
                {
            "ARM",
            "AZE",
            "BHR",
            "CYP",
            "GEO",
            "IRN",
            "IRQ",
            "ISR",
            "JOR",
            "KWT",
            "LBN",
            "OMN",
            "PSE",
            "QAT",
            "SAU",
            "SYR",
            "TUR",
            "ARE",
            "YEM"
                }
            );
        }

        EditorGUILayout.EndHorizontal();


        GUILayout.Space(10f);


        // ==================================================
        // ヨーロッパ
        // ==================================================

        EditorGUILayout.LabelField(
            "ヨーロッパ",
            EditorStyles.miniBoldLabel
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("06 ヨーロッパ①"))
        {
            SelectPreset(
                new string[]
                {
            "ALB",
            "AND",
            "AUT",
            "BLR",
            "BEL",
            "BIH",
            "BGR",
            "HRV",
            "CZE",
            "DNK",
            "EST",
            "FIN",
            "FRA",
            "DEU",
            "GRC",
            "HUN",
            "ISL",
            "IRL",
            "ITA",
            "LVA",
            "LIE",
            "LTU"
                }
            );
        }

        if (GUILayout.Button("07 ヨーロッパ②"))
        {
            SelectPreset(
                new string[]
                {
            "LUX",
            "MLT",
            "MDA",
            "MCO",
            "MNE",
            "NLD",
            "MKD",
            "NOR",
            "POL",
            "PRT",
            "ROU",
            "RUS",
            "SMR",
            "SRB",
            "SVK",
            "SVN",
            "ESP",
            "SWE",
            "CHE",
            "UKR",
            "GBR",
            "VAT"
                }
            );
        }

        EditorGUILayout.EndHorizontal();


        GUILayout.Space(10f);


        // ==================================================
        // アフリカ
        // ==================================================

        EditorGUILayout.LabelField(
            "アフリカ",
            EditorStyles.miniBoldLabel
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("08 アフリカ①"))
        {
            SelectPreset(
                new string[]
                {
            "DZA",
            "AGO",
            "BEN",
            "BWA",
            "BFA",
            "BDI",
            "CPV",
            "CMR",
            "CAF",
            "TCD",
            "COM",
            "COG",
            "COD",
            "CIV",
            "DJI",
            "EGY",
            "GNQ",
            "ERI"
                }
            );
        }

        if (GUILayout.Button("09 アフリカ②"))
        {
            SelectPreset(
                new string[]
                {
            "SWZ",
            "ETH",
            "GAB",
            "GMB",
            "GHA",
            "GIN",
            "GNB",
            "KEN",
            "LSO",
            "LBR",
            "LBY",
            "MDG",
            "MWI",
            "MLI",
            "MRT",
            "MUS",
            "MAR",
            "MOZ"
                }
            );
        }

        if (GUILayout.Button("10 アフリカ③"))
        {
            SelectPreset(
                new string[]
                {
            "NAM",
            "NER",
            "NGA",
            "RWA",
            "STP",
            "SEN",
            "SYC",
            "SLE",
            "SOM",
            "ZAF",
            "SSD",
            "SDN",
            "TZA",
            "TGO",
            "TUN",
            "UGA",
            "ZMB",
            "ZWE"
                }
            );
        }

        EditorGUILayout.EndHorizontal();


        GUILayout.Space(10f);


        // ==================================================
        // 北米・中央アメリカ・カリブ海
        // ==================================================

        EditorGUILayout.LabelField(
            "北米・中央アメリカ・カリブ海",
            EditorStyles.miniBoldLabel
        );

        if (GUILayout.Button("11 北米・中央アメリカ"))
        {
            SelectPreset(
                new string[]
                {
            "BLZ",
            "CAN",
            "CRI",
            "SLV",
            "GTM",
            "HND",
            "MEX",
            "NIC",
            "PAN",
            "USA"
                }
            );
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("12 カリブ海①"))
        {
            SelectPreset(
                new string[]
                {
            "ATG",
            "BHS",
            "BRB",
            "CUB",
            "DMA",
            "DOM",
            "GRD"
                }
            );
        }

        if (GUILayout.Button("13 カリブ海②"))
        {
            SelectPreset(
                new string[]
                {
            "HTI",
            "JAM",
            "KNA",
            "LCA",
            "VCT",
            "TTO"
                }
            );
        }

        EditorGUILayout.EndHorizontal();


        GUILayout.Space(10f);


        // ==================================================
        // 南米
        // ==================================================

        EditorGUILayout.LabelField(
            "南米",
            EditorStyles.miniBoldLabel
        );

        if (GUILayout.Button("14 南米"))
        {
            SelectPreset(
                new string[]
                {
            "ARG",
            "BOL",
            "BRA",
            "CHL",
            "COL",
            "ECU",
            "GUY",
            "PRY",
            "PER",
            "SUR",
            "URY",
            "VEN"
                }
            );
        }

        GUILayout.Space(10f);


        // ==================================================
        // オセアニア
        // ==================================================

        EditorGUILayout.LabelField(
            "オセアニア",
            EditorStyles.miniBoldLabel
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("15 オセアニア①"))
        {
            SelectPreset(
                new string[]
                {
            "AUS",
            "FJI",
            "KIR",
            "MHL",
            "FSM",
            "NRU",
            "NZL"
                }
            );
        }

        if (GUILayout.Button("16 オセアニア②"))
        {
            SelectPreset(
                new string[]
                {
            "PLW",
            "PNG",
            "WSM",
            "SLB",
            "TON",
            "TUV",
            "VUT"
                }
            );
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(15f);

        // ==================================================
        // 世界テーマ
        // ==================================================

        EditorGUILayout.LabelField(
            "世界テーマ",
            EditorStyles.miniBoldLabel
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("17 世界の主要国"))
        {
            SelectPreset(
                new string[]
                {
            "USA",
            "CHN",
            "JPN",
            "DEU",
            "GBR",
            "FRA",
            "ITA",
            "CAN",
            "IND",
            "BRA",
            "AUS",
            "KOR",
            "ESP",
            "MEX",
            "RUS",
            "TUR",
            "SAU",
            "IDN",
            "ARG",
            "ZAF"
                }
            );
        }

        if (GUILayout.Button("18 世界の大型国"))
        {
            SelectPreset(
                new string[]
                {
            "RUS",
            "CAN",
            "CHN",
            "USA",
            "BRA",
            "AUS",
            "IND",
            "ARG",
            "KAZ",
            "DZA",
            "COD",
            "SAU",
            "MEX",
            "IDN",
            "SDN"
                }
            );
        }

        if (GUILayout.Button("19 世界の島国"))
        {
            SelectPreset(
                new string[]
                {
            "JPN",
            "GBR",
            "ISL",
            "IRL",
            "MDG",
            "LKA",
            "IDN",
            "PHL",
            "NZL",
            "PNG",
            "CUB",
            "JAM",
            "FJI",
            "VUT",
            "MUS"
                }
            );
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10f);

        // ==================================================
        // 最終チャレンジ
        // ==================================================

        EditorGUILayout.LabelField(
            "最終チャレンジ",
            EditorStyles.miniBoldLabel
        );

        if (GUILayout.Button(
                "20 世界195か国",
                GUILayout.Height(35f)))
        {
            SelectAllCountries();
        }

        // ==================================================
        // 国一覧
        // ==================================================

        if (countries.Count > 0)
        {
            EditorGUILayout.LabelField(
                $"読み込み国数：{countries.Count}",
                EditorStyles.boldLabel
            );

            GUILayout.Space(5f);

            countryScrollPosition =
                EditorGUILayout.BeginScrollView(
                    countryScrollPosition,
                    GUILayout.Height(220f)
                );

            foreach (CountryEntry country in countries)
            {
                EditorGUILayout.BeginHorizontal();

                country.selected =
                    EditorGUILayout.Toggle(
                        country.selected,
                        GUILayout.Width(20f)
                    );

                EditorGUILayout.LabelField(
                    country.countryId,
                    GUILayout.Width(50f)
                );

                EditorGUILayout.LabelField(
                    country.countryName
                );

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10f);

            // ==================================================
            // 選択国数
            // ==================================================

            int selectedCount = 0;

            foreach (CountryEntry country in countries)
            {
                if (country.selected)
                {
                    selectedCount++;
                }
            }

            EditorGUILayout.LabelField(
                $"選択中の国数：{selectedCount}",
                EditorStyles.boldLabel
            );


            GUILayout.Space(5f);


            // ==================================================
            // 全解除
            // ==================================================

            if (GUILayout.Button("全ての選択を解除"))
            {
                foreach (CountryEntry country in countries)
                {
                    country.selected = false;
                }
            }


            GUILayout.Space(15f);


            // ==================================================
            // StageData作成
            // ==================================================

            GUI.enabled =
                selectedCount > 0 &&
                !string.IsNullOrWhiteSpace(stageId) &&
                !string.IsNullOrWhiteSpace(stageName);

            if (GUILayout.Button(
                    "Create StageData",
                    GUILayout.Height(40f)))
            {
                CreateStageData();
            }

            GUI.enabled = true;

            DrawPresetValidation();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "CountryData.csvを設定してください。",
                MessageType.Info
            );
        }

        // ==========================================
        // Window全体のScrollViewを終了
        // ==========================================

        EditorGUILayout.EndScrollView();
    }


    // ==================================================
    // CSV読み込み
    // ==================================================

    private void LoadCountriesFromCsv()
    {
        countries.Clear();

        if (countryCsv == null)
        {
            return;
        }

        string[] lines =
            countryCsv.text.Split(
                new[] { '\n', '\r' },
                System.StringSplitOptions.RemoveEmptyEntries
            );

        if (lines.Length <= 1)
        {
            Debug.LogWarning(
                "CountryData.csvにデータがありません。"
            );

            return;
        }


        // 1行目をヘッダーとして取得
        string[] headers =
            lines[0].Split(',');

        int idColumnIndex = -1;
        int nameColumnIndex = -1;


        // ADM0_A3 / NAME の列を探す
        for (int i = 0; i < headers.Length; i++)
        {
            string header =
                headers[i].Trim()
                    .Trim('\uFEFF')
                    .Trim('"');

            if (header == "id")
            {
                idColumnIndex = i;
            }

            if (header == "admin_name")
            {
                nameColumnIndex = i;
            }
        }


        if (idColumnIndex == -1)
        {
            Debug.LogError(
                "CountryData.csvに id 列が見つかりません。"
            );

            return;
        }

        if (nameColumnIndex == -1)
        {
            Debug.LogError(
                "CountryData.csvに admin_name 列が見つかりません。"
            );

            return;
        }

        // 2行目以降を読み込む
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values =
                lines[i].Split(',');

            if (values.Length <=
                Mathf.Max(
                    idColumnIndex,
                    nameColumnIndex
                ))
            {
                continue;
            }

            string countryId =
                values[idColumnIndex]
                    .Trim()
                    .Trim('"');

            string countryName =
                values[nameColumnIndex]
                    .Trim()
                    .Trim('"');


            if (string.IsNullOrEmpty(countryId))
            {
                continue;
            }


            CountryEntry entry =
                new CountryEntry();

            entry.countryId =
                countryId;

            entry.countryName =
                countryName;

            countries.Add(entry);
        }


        Debug.Log(
            $"StageDataCreator：{countries.Count}か国読み込みました。"
        );
    }

    // ==================================================
    // StageData作成
    // ==================================================

    private void CreateStageData()
    {
        // ------------------------------
        // 保存先を選択
        // ------------------------------

        string path =
            EditorUtility.SaveFilePanelInProject(
                "StageDataを保存",
                $"Stage_{stageId}",
                "asset",
                "StageDataの保存場所を選択してください。"
            );

        if (string.IsNullOrEmpty(path))
        {
            return;
        }


        // ------------------------------
        // StageData作成
        // ------------------------------

        StageData stageData =
            ScriptableObject.CreateInstance<StageData>();


        // 基本情報
        stageData.stageId =
            stageId;

        stageData.stageName =
            stageName;

        stageData.stageImage =
            stageImage;


        // 星評価
        stageData.threeStarTime =
            threeStarTime;

        stageData.twoStarTime =
            twoStarTime;


        // 地図開始位置
        stageData.mapStartPosition =
            mapStartPosition;

        stageData.mapStartScale =
            mapStartScale;


        // ------------------------------
        // 選択した国を登録
        // ------------------------------

        stageData.countryIds.Clear();

        foreach (CountryEntry country in countries)
        {
            if (!country.selected)
            {
                continue;
            }

            stageData.countryIds.Add(
                country.countryId
            );
        }


        // ------------------------------
        // Assetとして保存
        // ------------------------------

        AssetDatabase.CreateAsset(
            stageData,
            path
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        // 作成したStageDataを選択
        Selection.activeObject =
            stageData;

        EditorGUIUtility.PingObject(
            stageData
        );


        Debug.Log(
            $"StageDataを作成しました：" +
            $"{stageData.stageName} / " +
            $"{stageData.countryIds.Count}か国"
        );
    }

    // ==================================================
    // 地域プリセット選択
    // ==================================================

    private void SelectPreset(string[] countryIds)
    {
        // まず全部OFF
        foreach (CountryEntry country in countries)
        {
            country.selected = false;
        }

        // 指定された国だけON
        foreach (CountryEntry country in countries)
        {
            foreach (string countryId in countryIds)
            {
                if (country.countryId == countryId)
                {
                    country.selected = true;
                    break;
                }
            }
        }

        Repaint();
    }

    // ==================================================
    // 全地域プリセットの国IDを取得
    // ==================================================

    private List<string> GetAllPresetCountryIds()
    {
        return new List<string>()
    {
        // 01 東アジア
        "CHN", "JPN", "MNG", "PRK", "KOR",

        // 02 東南アジア
        "BRN", "KHM", "IDN", "LAO", "MYS",
        "MMR", "PHL", "SGP", "THA", "TLS", "VNM",

        // 03 南アジア
        "AFG", "BGD", "BTN", "IND",
        "MDV", "NPL", "PAK", "LKA",

        // 04 中央アジア
        "KAZ", "KGZ", "TJK", "TKM", "UZB",

        // 05 中東
        "ARM", "AZE", "BHR", "CYP", "GEO",
        "IRN", "IRQ", "ISR", "JOR", "KWT",
        "LBN", "OMN", "PSE", "QAT", "SAU",
        "SYR", "TUR", "ARE", "YEM",

        // 06 ヨーロッパ①
        "ALB", "AND", "AUT", "BLR", "BEL",
        "BIH", "BGR", "HRV", "CZE", "DNK",
        "EST", "FIN", "FRA", "DEU", "GRC",
        "HUN", "ISL", "IRL", "ITA", "LVA",
        "LIE", "LTU",

        // 07 ヨーロッパ②
        "LUX", "MLT", "MDA", "MCO", "MNE",
        "NLD", "MKD", "NOR", "POL", "PRT",
        "ROU", "RUS", "SMR", "SRB", "SVK",
        "SVN", "ESP", "SWE", "CHE", "UKR",
        "GBR", "VAT",

        // 08 アフリカ①
        "DZA", "AGO", "BEN", "BWA", "BFA",
        "BDI", "CPV", "CMR", "CAF", "TCD",
        "COM", "COG", "COD", "CIV", "DJI",
        "EGY", "GNQ", "ERI",

        // 09 アフリカ②
        "SWZ", "ETH", "GAB", "GMB", "GHA",
        "GIN", "GNB", "KEN", "LSO", "LBR",
        "LBY", "MDG", "MWI", "MLI", "MRT",
        "MUS", "MAR", "MOZ",

        // 10 アフリカ③
        "NAM", "NER", "NGA", "RWA", "STP",
        "SEN", "SYC", "SLE", "SOM", "ZAF",
        "SSD", "SDN", "TZA", "TGO", "TUN",
        "UGA", "ZMB", "ZWE",

        // 11 北米・中央アメリカ
        "BLZ", "CAN", "CRI", "SLV", "GTM",
        "HND", "MEX", "NIC", "PAN", "USA",

        // 12 カリブ海①
        "ATG", "BHS", "BRB", "CUB",
        "DMA", "DOM", "GRD",

        // 13 カリブ海②
        "HTI", "JAM", "KNA",
        "LCA", "VCT", "TTO",

        // 14 南米
        "ARG", "BOL", "BRA", "CHL",
        "COL", "ECU", "GUY", "PRY",
        "PER", "SUR", "URY", "VEN",

        // 15 オセアニア①
        "AUS", "FJI", "KIR", "MHL",
        "FSM", "NRU", "NZL",

        // 16 オセアニア②
        "PLW", "PNG", "WSM", "SLB",
        "TON", "TUV", "VUT"
    };
    }

    // ==================================================
    // 地域プリセット検査
    // ==================================================

    private void DrawPresetValidation()
    {
        if (countries.Count == 0)
        {
            return;
        }

        List<string> presetIds =
            GetAllPresetCountryIds();

        HashSet<string> csvIds =
            new HashSet<string>();

        foreach (CountryEntry country in countries)
        {
            csvIds.Add(country.countryId);
        }


        HashSet<string> uniquePresetIds =
            new HashSet<string>();

        List<string> duplicateIds =
            new List<string>();


        // 重複チェック
        foreach (string id in presetIds)
        {
            if (!uniquePresetIds.Add(id))
            {
                duplicateIds.Add(id);
            }
        }


        // 未分類チェック
        List<string> missingIds =
            new List<string>();

        foreach (string csvId in csvIds)
        {
            if (!uniquePresetIds.Contains(csvId))
            {
                missingIds.Add(csvId);
            }
        }


        // CSVに存在しないID
        List<string> invalidIds =
            new List<string>();

        foreach (string presetId in uniquePresetIds)
        {
            if (!csvIds.Contains(presetId))
            {
                invalidIds.Add(presetId);
            }
        }


        GUILayout.Space(20f);

        EditorGUILayout.LabelField(
            "地域プリセット検査",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            $"CSVの国数：{csvIds.Count}"
        );

        EditorGUILayout.LabelField(
            $"プリセット登録数：{presetIds.Count}"
        );

        EditorGUILayout.LabelField(
            $"登録済みユニーク国数：{uniquePresetIds.Count}"
        );

        EditorGUILayout.LabelField(
            $"未分類：{missingIds.Count}"
        );

        EditorGUILayout.LabelField(
            $"重複：{duplicateIds.Count}"
        );

        EditorGUILayout.LabelField(
            $"CSVに存在しないID：{invalidIds.Count}"
        );


        GUILayout.Space(5f);


        // ------------------------------
        // 完全一致
        // ------------------------------

        if (
            missingIds.Count == 0 &&
            duplicateIds.Count == 0 &&
            invalidIds.Count == 0
        )
        {
            EditorGUILayout.HelpBox(
                $"{csvIds.Count}か国すべてが、重複なしで地域プリセットに分類されています！",
                MessageType.Info
            );

            return;
        }


        // ------------------------------
        // 問題がある場合
        // ------------------------------

        if (missingIds.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "未分類：" +
                string.Join(", ", missingIds),
                MessageType.Warning
            );
        }

        if (duplicateIds.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "重複：" +
                string.Join(", ", duplicateIds),
                MessageType.Warning
            );
        }

        if (invalidIds.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "CSVに存在しないID：" +
                string.Join(", ", invalidIds),
                MessageType.Error
            );
        }
    }

    // ==================================================
    // CSVの全ての国を選択
    // ==================================================

    private void SelectAllCountries()
    {
        if (countries.Count == 0)
        {
            Debug.LogWarning(
                "CountryData.csvが読み込まれていません。"
            );

            return;
        }

        foreach (CountryEntry country in countries)
        {
            country.selected = true;
        }

        Debug.Log(
            $"全{countries.Count}か国を選択しました。"
        );

        Repaint();
    }

}