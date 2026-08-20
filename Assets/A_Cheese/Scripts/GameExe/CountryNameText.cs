using TMPro;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// countryIdをキーとして、現在の言語に対応した国名を表示する。
/// </summary>
public class CountryNameText : MonoBehaviour
{
    [Header("国ID")]
    [SerializeField]
    private string countryId;

    [Header("国名を表示するText")]
    [SerializeField]
    private TMP_Text countryNameText;

    private LocalizedString localizedCountryName;
    private bool isSubscribed;

    /// <summary>
    /// CountryPieceGeneratorから呼ばれる。
    /// defaultNameは既存処理との互換性のため、現在は残している。
    /// </summary>
    public void Setup(
        string id,
        string defaultName
    )
    {
        Unsubscribe();

        countryId = id;
        localizedCountryName = null;

        InitializeLocalizedString();
        Subscribe();
    }

    private void OnEnable()
    {
        InitializeLocalizedString();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>
    /// 保存されているcountryIdから、
    /// ゲーム開始時にもLocalizedStringを作成する。
    /// </summary>
    private void InitializeLocalizedString()
    {
        if (localizedCountryName != null)
        {
            return;
        }

        if (string.IsNullOrEmpty(countryId))
        {
            return;
        }

        localizedCountryName = new LocalizedString(
            "CountryNames",
            countryId
        );
    }

    private void Subscribe()
    {
        if (localizedCountryName == null ||
            isSubscribed)
        {
            return;
        }

        localizedCountryName.StringChanged +=
            UpdateCountryName;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (localizedCountryName == null ||
            !isSubscribed)
        {
            return;
        }

        localizedCountryName.StringChanged -=
            UpdateCountryName;

        isSubscribed = false;
    }

    private void UpdateCountryName(
        string localizedName
    )
    {
        if (countryNameText != null)
        {
            countryNameText.text =
                localizedName;
        }
    }
}