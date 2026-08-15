using TMPro;
using UnityEngine;

/// <summary>
/// 国名表示を管理する。
/// 今は英語名を表示し、
/// 将来Localization対応するためにCountryIdを保持する。
/// </summary>
public class CountryNameText : MonoBehaviour
{
    [Header("国ID")]
    [SerializeField]
    private string countryId;

    [Header("国名を表示するText")]
    [SerializeField]
    private TMP_Text countryNameText;

    /// <summary>
    /// CountryPieceGeneratorから呼ぶ。
    /// </summary>
    public void Setup(
        string id,
        string defaultName
    )
    {
        countryId = id;

        if (countryNameText != null)
        {
            countryNameText.text =
                defaultName;
        }
    }
}