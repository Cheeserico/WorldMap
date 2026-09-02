using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 対応している言語を一覧表示して切り替える。
/// </summary>
public class LanguageSelectionPopup : MonoBehaviour
{
    [Header("言語一覧")]
    [SerializeField]
    private RectTransform content;

    [SerializeField]
    private LanguageItemUI languageItemTemplate;

    private readonly List<LanguageItemUI>
        generatedItems =
            new List<LanguageItemUI>();

    private readonly List<Locale>
        generatedLocales =
            new List<Locale>();

    private IEnumerator Start()
    {
        // Localizationの初期化完了を待つ
        yield return
            LocalizationSettings
                .InitializationOperation;

        BuildLanguageItems();
    }

    private void BuildLanguageItems()
    {
        if (content == null ||
            languageItemTemplate == null)
        {
            Debug.LogError(
                "LanguageSelectionPopup：" +
                "ContentまたはTemplateが設定されていません。",
                this
            );

            return;
        }

        languageItemTemplate
            .gameObject
            .SetActive(false);

        generatedItems.Clear();
        generatedLocales.Clear();

        foreach (
            Locale locale in
            LocalizationSettings
                .AvailableLocales
                .Locales)
        {
            Locale capturedLocale =
                locale;

            LanguageItemUI item =
                Instantiate(
                    languageItemTemplate,
                    content
                );

            item.gameObject.SetActive(true);

            bool isSelected =
                LocalizationSettings
                    .SelectedLocale ==
                capturedLocale;

            item.Setup(
                GetNativeLanguageName(
                    capturedLocale
                ),
                isSelected,
                () =>
                    SelectLanguage(
                        capturedLocale
                    )
            );

            generatedItems.Add(item);
            generatedLocales.Add(
                capturedLocale
            );
        }
    }

    private void SelectLanguage(
        Locale locale
    )
    {
        if (locale == null)
        {
            return;
        }

        if (LocalizationManager.Instance ==
            null)
        {
            Debug.LogError(
                "LanguageSelectionPopup：" +
                "LocalizationManagerが見つかりません。",
                this
            );

            return;
        }

        LocalizationManager.Instance.SetLocale(
            locale.Identifier.Code
        );

        RefreshSelectedMarks();

        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.Close(
                PopupType.Language
            );
        }
    }

    private void RefreshSelectedMarks()
    {
        Locale selectedLocale =
            LocalizationSettings.SelectedLocale;

        for (
            int i = 0;
            i < generatedItems.Count;
            i++
        )
        {
            generatedItems[i].SetSelected(
                generatedLocales[i] ==
                selectedLocale
            );
        }
    }

    /// <summary>
    /// 言語名を、その言語自身の表記で返す。
    /// </summary>
    public static string GetNativeLanguageName(
        Locale locale
    )
    {
        string code =
            locale.Identifier.Code;

        switch (code)
        {
            case "ar": return "العربية";
            case "bn": return "বাংলা";
            case "zh-Hans": return "简体中文";
            case "zh-Hant": return "繁體中文";
            case "nl": return "Nederlands";
            case "en": return "English";
            case "fr": return "Français";
            case "de": return "Deutsch";
            case "hi": return "हिन्दी";
            case "id": return "Bahasa Indonesia";
            case "it": return "Italiano";
            case "ja": return "日本語";
            case "ko": return "한국어";
            case "mr-IN": return "मराठी";
            case "pt": return "Português";
            case "ru": return "Русский";
            case "es": return "Español";
            case "sv": return "Svenska";
            case "ta": return "தமிழ்";
            case "te": return "తెలుగు";
            case "th": return "ไทย";
            case "tr": return "Türkçe";
            case "uk": return "Українська";
            default:
                return locale.LocaleName;
        }
    }
}