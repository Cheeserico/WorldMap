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

    // StageDataから自動設定される出題国数
    private int totalCountryCount;

    // 現在正解している国の数
    private int placedCountryCount;

    private void Start()
    {
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

        if (placedCountryCount >= totalCountryCount)
        {
            OnGameClear();
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
                stageData.countryIds.Contains(piece.CountryId);

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
                stageData.countryIds.Contains(slot.CountryId);

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

        totalCountryCount = stageData.countryIds.Count;

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
}