using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("獲得した星")]
    [SerializeField]
    private GameObject[] yellowStars;


    [Header("ゲームScene名")]
    [SerializeField]
    private string gameSceneName = "GameScene";




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

        if (stageNameText != null)
        {
            stageNameText.text =
                stageData.stageName;
        }

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
                "BEST --:--.--";

            UpdateStars(0);

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
            $"BEST {FormatTime(bestTime)}";

        int starCount =
            CalculateStarCount(bestTime);

        UpdateStars(starCount);
    }

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
    // ステージ選択
    // ==================================================

    public void SelectStage()
    {
        if (stageData == null)
        {
            Debug.LogWarning(
                "StageSelectButton：StageDataが設定されていません。"
            );

            return;
        }

        if (StageManager.Instance == null)
        {
            Debug.LogError(
                "StageSelectButton：StageManagerが見つかりません。"
            );

            return;
        }

        // 選択したStageDataを保存
        StageManager.Instance.SetStageData(
            stageData
        );

        Debug.Log(
            $"StageSelectButton：{stageData.name} を選択しました。"
        );

        // GameSceneへ移動
        SceneManager.LoadScene(
            gameSceneName
        );
    }
}