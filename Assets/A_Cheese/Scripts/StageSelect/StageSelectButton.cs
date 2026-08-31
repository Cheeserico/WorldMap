using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization;

/// <summary>
/// ステージ選択画面の各ボタンにつける。
/// StageDataの情報やベストタイムを表示し、
/// 選択されたStageDataをStageManagerへ渡す。
/// </summary>
public class StageSelectButton : MonoBehaviour
{
    [Header("このボタンで選択するステージ")]
    [SerializeField]
    private StageData stageData;


    [Header("表示")]
    [SerializeField]
    private TextMeshProUGUI stageNameText;

    [SerializeField]
    private TextMeshProUGUI bestTimeText;

    [SerializeField]
    private Image stageImage;

    [Header("クリア済み表示")]
    [SerializeField]
    private GameObject clearBadge;

    [Header("獲得した星")]
    [SerializeField]
    private GameObject[] yellowStars;

    [Header("ステージ名ローカライズ")]
    private LocalizedString localizedStageName;

    private bool isStageNameSubscribed;

    [Header("ゲームScene名")]
    [SerializeField]
    private string gameSceneName = "GameScene";


    private void OnEnable()
    {
        InitializeStageNameLocalization();
        SubscribeToStageName();
    }

    private void OnDisable()
    {
        UnsubscribeFromStageName();
    }

    // ==================================================
    // 初期表示
    // ==================================================

    private void Start()
    {
        UpdateDisplay();
    }


    // ==================================================
    // 表示更新
    // ==================================================

    private void UpdateDisplay()
    {
        if (stageData == null)
        {
            Debug.LogWarning(
                "StageSelectButton：StageDataが設定されていません。"
            );

            return;
        }

        // ------------------------------
        // ステージ名
        // ------------------------------

        InitializeStageNameLocalization();
        SubscribeToStageName();


        // イメージ
        if (stageImage != null)
        {
            stageImage.sprite = stageData.stageImage;
        }

        // ------------------------------
        // ベストタイム
        // ------------------------------

        UpdateBestTimeDisplay();
    }


    // ==================================================
    // ステージ名ローカライズ
    // ==================================================

    private void InitializeStageNameLocalization()
    {
        if (localizedStageName != null)
        {
            return;
        }

        if (stageData == null ||
            string.IsNullOrEmpty(stageData.stageId))
        {
            return;
        }

        localizedStageName = new LocalizedString(
            "StageNames",
            stageData.stageId
        );
    }

    private void SubscribeToStageName()
    {
        if (localizedStageName == null ||
            isStageNameSubscribed)
        {
            return;
        }

        localizedStageName.StringChanged +=
            UpdateLocalizedStageName;

        isStageNameSubscribed = true;
    }

    private void UnsubscribeFromStageName()
    {
        if (localizedStageName == null ||
            !isStageNameSubscribed)
        {
            return;
        }

        localizedStageName.StringChanged -=
            UpdateLocalizedStageName;

        isStageNameSubscribed = false;
    }

    private void UpdateLocalizedStageName(
        string localizedName
    )
    {
        if (stageNameText != null)
        {
            stageNameText.text =
                localizedName;
        }

        UpdateBestTimeDisplay();
    }
    
    // ==================================================
    // ベストタイム表示
    // ==================================================

    private void UpdateBestTimeDisplay()
    {
        if (bestTimeText == null)
        {
            return;
        }

        string bestTimeKey =
            SaveKeys.GetBestTimeKey(
                stageData.stageId
            );

        // ------------------------------
        // 未クリア
        // ------------------------------

        if (!PlayerPrefs.HasKey(bestTimeKey))
        {
            bestTimeText.text =
                "--:--.--";

            UpdateStars(0);
            UpdateClearBadge(false);

            return;
        }
        
        // ------------------------------
        // クリア済み
        // ------------------------------

        float bestTime =
            PlayerPrefs.GetFloat(
                bestTimeKey
            );

        bestTimeText.text =
            FormatTime(bestTime);

        int starCount =
            CalculateStarCount(bestTime);

        UpdateStars(starCount);
        UpdateClearBadge(true);
    }



    /*
    // ==================================================
    // ベストタイムのローカライズ
    // ==================================================

    private string GetLocalizedBestTimeText(
        string timeText
    )
    {
        LocalizedString localizedBestTime =
            new LocalizedString(
                "UITexts",
                "stage_best_time"
            );

        localizedBestTime.Arguments =
            new object[]
            {
                timeText
            };

        return
            localizedBestTime.GetLocalizedString();
    }
    */

    // ==================================================
    // 時間表示
    // ==================================================

    private string FormatTime(float time)
    {
        int minutes =
            Mathf.FloorToInt(time / 60f);

        int seconds =
            Mathf.FloorToInt(time % 60f);

        int centiseconds =
            Mathf.FloorToInt(
                (time - Mathf.Floor(time))
                * 100f
            );

        return
            $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }

    private int CalculateStarCount(float time)
    {
        if (time <= stageData.threeStarTime)
        {
            return 3;
        }

        if (time <= stageData.twoStarTime)
        {
            return 2;
        }

        return 1;
    }

    private void UpdateStars(int starCount)
    {
        if (yellowStars == null)
        {
            return;
        }

        for (int i = 0; i < yellowStars.Length; i++)
        {
            if (yellowStars[i] == null)
            {
                continue;
            }

            yellowStars[i].SetActive(
                i < starCount
            );
        }
    }

    // ==================================================
    // クリア済み表示
    // ==================================================

    private void UpdateClearBadge(
        bool isCleared
    )
    {
        if (clearBadge == null)
        {
            return;
        }

        clearBadge.SetActive(
            isCleared
        );
    }

    // ==================================================
    // ステージ選択
    // ==================================================

    public void SelectStage()
    {
        if (stageData == null)
        {
            Debug.LogWarning(
                "StageSelectButton：" +
                "StageDataが設定されていません。"
            );

            return;
        }

        if (StageManager.Instance == null)
        {
            Debug.LogError(
                "StageSelectButton：" +
                "StageManagerが見つかりません。"
            );

            return;
        }

        bool supportsContinueSave =
            ContinueSaveManager.SupportsContinueSave(
                stageData
            );

        bool hasContinueSave =
            stageData.showContinuePopup &&
            supportsContinueSave &&
            ContinueSaveManager.HasSaveData(
                stageData.stageId
            );


        // ==================================================
        // 途中データがある場合
        // ==================================================

        if (hasContinueSave)
        {
            if (PopupManager.Instance == null)
            {
                Debug.LogError(
                    "StageSelectButton：" +
                    "PopupManagerが見つかりません。"
                );

                return;
            }

            ContinueStagePopup continuePopup =
                PopupManager.Instance
                    .GetPopup<ContinueStagePopup>(
                        PopupType.ContinueStage
                    );

            if (continuePopup == null)
            {
                Debug.LogError(
                    "StageSelectButton：" +
                    "ContinueStagePopupが取得できません。"
                );

                return;
            }

            continuePopup.Setup(
                stageData,
                gameSceneName
            );

            PopupManager.Instance.Open(
                PopupType.ContinueStage
            );

            Debug.Log(
                $"途中データがあります：" +
                $"{stageData.stageId}"
            );

            return;
        }

        // ==================================================
        // 途中データがない場合
        // ==================================================

        StageManager.Instance.SetStageData(
            stageData
        );

        StageManager.Instance.SetStartMode(
            StageStartMode.NewGame
        );

        Debug.Log(
            $"StageSelectButton：" +
            $"{stageData.name}を最初から開始します。"
        );

        SceneManager.LoadScene(
            gameSceneName
        );
    }

}