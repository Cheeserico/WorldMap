using TMPro;
using UnityEngine;

/// <summary>
/// 世界地図パズル全体の進行状況を管理する。
/// </summary>
public class CountryPuzzleManager : MonoBehaviour
{
    [Header("進捗を表示するText")]
    [SerializeField]
    private TextMeshProUGUI progressText;

    [Header("今回配置する国の総数")]
    [SerializeField]
    private int totalCountryCount = 3;

    [Header("タイマー")]
    [SerializeField]
    private TimerManager timerManager;


    // 現在正解している国の数
    private int placedCountryCount;

    private void Start()
    {
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



}