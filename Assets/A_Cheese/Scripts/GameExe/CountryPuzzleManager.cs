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

    /// <summary>
    /// 全問正解して、クリア処理が確定しているか。
    /// </summary>
    public bool IsGameClearScheduled =>
        isGameClearScheduled;

    // ==================================================
    // 今回実際に出題する国
    // ==================================================

    private List<string> activeCountryIds =
        new List<string>();


    // ==================================================
    // 配置済みの国ID
    // ==================================================

    private List<string> placedCountryIds =
        new List<string>();

    /// <summary>
    /// 現在配置済みになっている国ID一覧のコピーを取得する。
    /// </summary>
    public List<string> GetPlacedCountryIds()
    {
        return new List<string>(
            placedCountryIds
        );
    }

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

        placedCountryIds.Clear();

        placedCountryCount = 0;
        UpdateProgressText();
    }

    /// <summary>
    /// 国ピースが正しく配置されたときに呼ぶ。
    /// </summary>
    /// <summary>
    /// 国ピースが正しく配置されたときに呼ぶ。
    /// </summary>
    public void AddPlacedCountry(
        string countryId
    )
    {
        if (string.IsNullOrEmpty(countryId))
        {
            Debug.LogWarning(
                "CountryPuzzleManager：" +
                "配置したCountryIdが空です。"
            );

            return;
        }

        if (!activeCountryIds.Contains(countryId))
        {
            Debug.LogWarning(
                $"CountryPuzzleManager：" +
                $"{countryId}は今回の出題国ではありません。"
            );

            return;
        }

        // 同じ国を二重加算しない
        if (placedCountryIds.Contains(countryId))
        {
            Debug.LogWarning(
                $"CountryPuzzleManager：" +
                $"{countryId}はすでに配置済みです。"
            );

            return;
        }

        placedCountryIds.Add(
            countryId
        );

        placedCountryCount =
            placedCountryIds.Count;

        // 念のため総数を超えないようにする
        placedCountryCount = Mathf.Clamp(
            placedCountryCount,
            0,
            totalCountryCount
        );

        Debug.Log(
            $"配置済み：{placedCountryCount} " +
            $"/ {totalCountryCount} " +
            $"追加国={countryId}"
        );

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
    /// 保存されていた国ID一覧から、
    /// 配置済みピースと進捗を復元する。
    /// </summary>
    public int RestorePlacedCountries(
        List<string> savedCountryIds
    )
    {
        placedCountryIds.Clear();
        placedCountryCount = 0;

        isGameClearScheduled = false;

        if (savedCountryIds == null ||
            savedCountryIds.Count == 0)
        {
            UpdateProgressText();
            return 0;
        }

        CountrySlot[] slots =
            FindObjectsByType<CountrySlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        CountryPieceDrag[] pieces =
            FindObjectsByType<CountryPieceDrag>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        Dictionary<string, CountrySlot> slotById =
            new Dictionary<string, CountrySlot>();

        Dictionary<string, CountryPieceDrag> pieceById =
            new Dictionary<string, CountryPieceDrag>();

        // 国IDからSlotを取得できるようにする
        foreach (CountrySlot slot in slots)
        {
            if (slot == null ||
                string.IsNullOrEmpty(slot.CountryId))
            {
                continue;
            }

            if (!slotById.ContainsKey(slot.CountryId))
            {
                slotById.Add(
                    slot.CountryId,
                    slot
                );
            }
        }

        // 国IDからPieceを取得できるようにする
        foreach (CountryPieceDrag piece in pieces)
        {
            if (piece == null ||
                string.IsNullOrEmpty(piece.CountryId))
            {
                continue;
            }

            if (!pieceById.ContainsKey(piece.CountryId))
            {
                pieceById.Add(
                    piece.CountryId,
                    piece
                );
            }
        }

        foreach (string countryId in savedCountryIds)
        {
            if (string.IsNullOrEmpty(countryId))
            {
                continue;
            }

            // 同じIDが保存されていても二重復元しない
            if (placedCountryIds.Contains(countryId))
            {
                continue;
            }

            // 今回のステージに含まれない国は復元しない
            if (!activeCountryIds.Contains(countryId))
            {
                Debug.LogWarning(
                    $"復元対象外の国です：{countryId}"
                );

                continue;
            }

            if (!slotById.TryGetValue(
                    countryId,
                    out CountrySlot slot))
            {
                Debug.LogWarning(
                    $"復元用Slotが見つかりません：{countryId}"
                );

                continue;
            }

            if (!pieceById.TryGetValue(
                    countryId,
                    out CountryPieceDrag piece))
            {
                Debug.LogWarning(
                    $"復元用Pieceが見つかりません：{countryId}"
                );

                continue;
            }

            bool restored =
                slot.RestorePlacedCountry(
                    piece
                );

            if (!restored)
            {
                continue;
            }

            placedCountryIds.Add(
                countryId
            );
        }

        placedCountryCount =
            Mathf.Clamp(
                placedCountryIds.Count,
                0,
                totalCountryCount
            );

        UpdateProgressText();

        Debug.Log(
            $"途中進捗を復元しました。 " +
            $"{placedCountryCount} / {totalCountryCount}"
        );

        return placedCountryCount;
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

        List<string> candidateCountryIds =
            new List<string>();


        // --------------------------------------------------
        // countryIdsが設定されている場合
        // → その中だけをランダム候補にする
        // --------------------------------------------------

        if (stageData.countryIds != null &&
            stageData.countryIds.Count > 0)
        {
            foreach (string countryId in stageData.countryIds)
            {
                if (string.IsNullOrEmpty(countryId))
                {
                    continue;
                }

                if (!candidateCountryIds.Contains(countryId))
                {
                    candidateCountryIds.Add(
                        countryId
                    );
                }
            }

            Debug.Log(
                $"地域限定ランダム：" +
                $"{candidateCountryIds.Count}か国を候補にします。"
            );
        }


        // --------------------------------------------------
        // countryIdsが空の場合
        // → Scene内の全CountrySlotを候補にする
        // --------------------------------------------------

        else
        {
            CountrySlot[] slots =
                FindObjectsByType<CountrySlot>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (CountrySlot slot in slots)
            {
                if (string.IsNullOrEmpty(slot.CountryId))
                {
                    continue;
                }

                if (!candidateCountryIds.Contains(slot.CountryId))
                {
                    candidateCountryIds.Add(
                        slot.CountryId
                    );
                }
            }

            Debug.Log(
                $"全世界ランダム：" +
                $"{candidateCountryIds.Count}か国を候補にします。"
            );
        }


        // --------------------------------------------------
        // 候補が0なら終了
        // --------------------------------------------------

        if (candidateCountryIds.Count == 0)
        {
            Debug.LogError(
                "ランダム抽選の候補国がありません。"
            );

            return;
        }


        // --------------------------------------------------
        // 抽選数を決定
        // --------------------------------------------------

        int randomCount =
            Mathf.Clamp(
                stageData.randomCountryCount,
                1,
                candidateCountryIds.Count
            );


        // --------------------------------------------------
        // 重複なしランダム抽選
        // --------------------------------------------------

        for (int i = 0; i < randomCount; i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    candidateCountryIds.Count
                );

            activeCountryIds.Add(
                candidateCountryIds[randomIndex]
            );

            candidateCountryIds.RemoveAt(
                randomIndex
            );
        }


        // --------------------------------------------------
        // ログ
        // --------------------------------------------------

        Debug.Log(
            $"ランダムステージ：" +
            $"{activeCountryIds.Count}か国を抽選しました。"
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