using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// ゲーム全体の言語設定を管理する。
///
/// ・現在言語の変更
/// ・選択言語のPlayerPrefs保存
/// ・アプリ起動時の言語復元
/// ・Sceneをまたいだ言語設定の維持
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance
    {
        get;
        private set;
    }

    public bool IsInitialized
    {
        get;
        private set;
    }

    public string CurrentLocaleCode
    {
        get
        {
            Locale selectedLocale =
                LocalizationSettings.SelectedLocale;

            if (selectedLocale == null)
            {
                return string.Empty;
            }

            return selectedLocale.Identifier.Code;
        }
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        // Localizationの初期化完了を待つ
        yield return
            LocalizationSettings.InitializationOperation;

        LoadSavedLocale();

        IsInitialized = true;
    }

    /// <summary>
    /// Settings画面などから言語を変更する。
    /// localeCodeにはen、jaなどを渡す。
    /// </summary>
    public void SetLocale(
        string localeCode
    )
    {
        Locale locale =
            FindLocale(localeCode);

        if (locale == null)
        {
            Debug.LogWarning(
                $"LocalizationManager：" +
                $"Localeが見つかりません。{localeCode}"
            );

            return;
        }

        LocalizationSettings.SelectedLocale =
            locale;

        PlayerPrefs.SetString(
            SaveKeys.SelectedLocale,
            locale.Identifier.Code
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"言語を変更しました：" +
            $"{locale.Identifier.Code}"
        );
    }

    /// <summary>
    /// 保存されている言語を復元する。
    /// 初回起動で保存がなければ、
    /// Unity Localizationのシステム言語判定をそのまま使う。
    /// </summary>
    private void LoadSavedLocale()
    {
        if (!PlayerPrefs.HasKey(
                SaveKeys.SelectedLocale))
        {
            return;
        }

        string savedLocaleCode =
            PlayerPrefs.GetString(
                SaveKeys.SelectedLocale
            );

        Locale savedLocale =
            FindLocale(savedLocaleCode);

        if (savedLocale == null)
        {
            Debug.LogWarning(
                $"保存されたLocaleが見つかりません：" +
                $"{savedLocaleCode}"
            );

            return;
        }

        LocalizationSettings.SelectedLocale =
            savedLocale;

        Debug.Log(
            $"保存言語を復元しました：" +
            $"{savedLocale.Identifier.Code}"
        );
    }

    private Locale FindLocale(
        string localeCode
    )
    {
        foreach (
            Locale locale in
            LocalizationSettings
                .AvailableLocales
                .Locales)
        {
            if (locale.Identifier.Code ==
                localeCode)
            {
                return locale;
            }
        }

        return null;
    }
}