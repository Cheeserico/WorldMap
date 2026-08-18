using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 世界地図パズル全体の進行状況を管理する。
/// </summary>
public class CountryPuzzleManager : MonoBehaviour
{
    [Header("ステージデータ")]
    [SerializeField]
    private StageData stageData;
    public StageData CurrentStageData => stageData;



    [Header("進捗を表示するText")]
    [SerializeField]
    private TextMeshProUGUI progressText;


    [Header("タイマー")]
    [SerializeField]
    private TimerManager timerManager;

    [Header("ゲームクリア演出")]
    [SerializeField]
    private float resultDelay = 1.4f;

    private Tween resultDelayTween;
    private bool isGameClearScheduled;

    // ==================================================
    // 今回実際に出題する国
    // ==================================================

    private List<string> activeCountryIds =
        new List<string>();


    // StageDataから自動設定される出題国数
    private int totalCountryCount;

    // 現在正解している国の数
    private int placedCountryCount;

    private void Start()
    {
        // StageSelectSceneで選ばれたStageDataを受け取る
        if (StageManager.Instance != null &&
            StageManager.Instance.SelectedStageData != null)
        {
            stageData = StageManager.Instance.SelectedStageData;

            Debug.Log(
                $"CountryPuzzleManager：{stageData.name} を読み込みました。"
            );
        }
        else
        {
            Debug.LogWarning(
                "CountryPuzzleManager：StageManagerにStageDataが設定されていません。InspectorのStageDataを使用します。"
            );
        }

        // 今回実際に出題する国を準備
        PrepareActiveCountryIds();

        ApplyStageCountryCount();

        ApplyStageToSlots();
        ApplyStageToPieces();

        placedCountryCount = 0;
        UpdateProgressText();
    }

    /// <summary>
    /// 国ピースが正しく配置されたときに呼ぶ。
    /// </summary>
    public void AddPlacedCountry()
    {
        placedCountryCount++;

        // 念のため総数を超えないようにする
        placedCountryCount = Mathf.Clamp(
            placedCountryCount,
            0,
            totalCountryCount
        );

        Debug.Log($"配置済み：{placedCountryCount} / {totalCountryCount}");

        UpdateProgressText();

        if (
            placedCountryCount >= totalCountryCount &&
            !isGameClearScheduled
        )
        {
            isGameClearScheduled = true;

            // Resultの待機中もクリアタイムが増えないよう即停止
            if (timerManager != null)
            {
                timerManager.StopTimer();
            }

            resultDelayTween?.Kill();

            resultDelayTween =
                DOVirtual.DelayedCall(
                    resultDelay,
                    OnGameClear
                );
        }
    }

    /// <summary>
    /// 全ての国を配置したときの処理。
    /// </summary>
    private void OnGameClear()
    {
        Debug.Log("ゲームクリア！");

        if (ResultManager.Instance != null)
        {
            ResultManager.Instance.ShowResult();
        }
    }

    /// <summary>
    /// 進捗表示を更新する。
    /// </summary>
    private void UpdateProgressText()
    {
        if (progressText == null)
        {
            return;
        }

        progressText.text =
            $"{placedCountryCount} / {totalCountryCount}";
    }

    // CountryPuzzleManagerがStageDataを読んで、各CountrySlotの色を自動設定する処理
    // StageDataに登録されている国のPieceだけ表示する
    private void ApplyStageToPieces()
    {
        if (stageData == null)
        {
            Debug.LogWarning("StageData が設定されていません。");
            return;
        }

        CountryPieceDrag[] pieces = FindObjectsByType<CountryPieceDrag>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (CountryPieceDrag piece in pieces)
        {
            bool isStageCountry =
                activeCountryIds.Contains(piece.CountryId);

            piece.gameObject.SetActive(isStageCountry);
        }
    }

    // 下のピースを出題国だけ表示
    private void ApplyStageToSlots()
    {
        if (stageData == null)
        {
            Debug.LogWarning("StageData が設定されていません。");
            return;
        }

        CountrySlot[] slots = FindObjectsByType<CountrySlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (CountrySlot slot in slots)
        {
            bool isStageCountry =
                activeCountryIds.Contains(slot.CountryId);

            slot.SetStageActive(isStageCountry);
        }
    }

    // StageDataから国数を取得するメソッド
    private void ApplyStageCountryCount()
    {
        if (stageData == null)
        {
            Debug.LogError("StageData が設定されていません。");
            totalCountryCount = 0;
            return;
        }

        totalCountryCount =
            activeCountryIds.Count;

        Debug.Log(
            $"ステージ出題数：{totalCountryCount}"
        );
    }

    // StagedetaからIDと名前取得
    public string CurrentStageId
    {
        get
        {
            if (stageData == null)
            {
                return "";
            }

            return stageData.stageId;
        }
    }

    public string CurrentStageName
    {
        get
        {
            if (stageData == null)
            {
                return "";
            }

            return stageData.stageName;
        }
    }

    // ==================================================
    // 今回出題する国を準備
    // ==================================================

    private void PrepareActiveCountryIds()
    {
        activeCountryIds.Clear();

        if (stageData == null)
        {
            Debug.LogError(
                "StageData が設定されていません。"
            );

            return;
        }

        // ==================================================
        // 通常ステージ
        // ==================================================

        if (!stageData.useRandomCountries)
        {
            activeCountryIds.AddRange(
                stageData.countryIds
            );

            Debug.Log(
                $"通常ステージ：{activeCountryIds.Count}か国を使用します。"
            );

            return;
        }

        // ==================================================
        // ランダムステージ
        // ==================================================

        CountrySlot[] slots =
            FindObjectsByType<CountrySlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        List<string> allCountryIds =
            new List<string>();

        foreach (CountrySlot slot in slots)
        {
            if (string.IsNullOrEmpty(slot.CountryId))
            {
                continue;
            }

            if (!allCountryIds.Contains(slot.CountryId))
            {
                allCountryIds.Add(
                    slot.CountryId
                );
            }
        }

        if (allCountryIds.Count == 0)
        {
            Debug.LogError(
                "ランダム抽選用のCountrySlotが見つかりません。"
            );

            return;
        }

        int randomCount =
            Mathf.Clamp(
                stageData.randomCountryCount,
                1,
                allCountryIds.Count
            );

        // 重複なしでランダム抽選
        for (int i = 0; i < randomCount; i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    allCountryIds.Count
                );

            activeCountryIds.Add(
                allCountryIds[randomIndex]
            );

            allCountryIds.RemoveAt(
                randomIndex
            );
        }

        Debug.Log(
            $"ランダムステージ：{activeCountryIds.Count}か国を抽選しました。"
        );

        Debug.Log(
            "抽選国：" +
            string.Join(", ", activeCountryIds)
        );
    }

    private void OnDestroy()
    {
        resultDelayTween?.Kill();
    }

}