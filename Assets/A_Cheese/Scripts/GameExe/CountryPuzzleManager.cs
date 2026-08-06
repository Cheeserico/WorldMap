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
    private int totalCountryCount = 1;

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
        placedCountryCount =
            Mathf.Clamp(
                placedCountryCount,
                0,
                totalCountryCount
            );

        UpdateProgressText();

        if (placedCountryCount >= totalCountryCount)
        {
            Debug.Log("すべての国を配置しました！");

            // 全問クリアSE
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(SEType.Clear);
            }
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