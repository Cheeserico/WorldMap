using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SettingsPopupのルートなど、ボタンを隠しても有効なままの場所に付ける。
/// 必要な場合だけ広告のプライバシー設定ボタンを表示する。
/// ButtonのOnClickはコードで登録するため、Inspectorでの登録は不要。
/// </summary>
[DisallowMultipleComponent]
public sealed class AdPrivacySettingsUI : MonoBehaviour
{
    [Header("広告のプライバシー設定")]
    [Tooltip("設定画面内に作成した専用Buttonを指定してください。")]
    [SerializeField] private Button privacySettingsButton;

    private void Awake()
    {
        if (privacySettingsButton == null)
        {
            Debug.LogWarning("[Ads] Privacy Settings Buttonを設定してください。", this);
            enabled = false;
            return;
        }

        // 自分自身や親を隠すと、その後ボタンを再表示できなくなる。
        if (transform.IsChildOf(privacySettingsButton.transform))
        {
            Debug.LogError("[Ads] AdPrivacySettingsUIはボタンではなく、SettingsPopupのルートに付けてください。", this);
            enabled = false;
            return;
        }

        privacySettingsButton.onClick.AddListener(OnPrivacySettingsClicked);
    }

    private void OnEnable()
    {
        RefreshButton();
    }

    private void Update()
    {
        // 設定を開いた後で同意情報の更新が完了した場合にも追従する。
        // Time.timeScaleが0でも更新する。
        RefreshButton();
    }

    private void RefreshButton()
    {
        if (privacySettingsButton == null) return;
        AdManager manager = AdManager.Instance;
        bool required = manager != null && manager.PrivacyOptionsRequired;
        if (privacySettingsButton.gameObject.activeSelf != required)
            privacySettingsButton.gameObject.SetActive(required);

        privacySettingsButton.interactable = required && manager.CanShowPrivacyOptions;
    }

    private void OnPrivacySettingsClicked()
    {
        AdManager manager = AdManager.Instance;
        if (manager != null) manager.ShowPrivacyOptions();
        RefreshButton();
    }

    private void OnDestroy()
    {
        if (privacySettingsButton != null)
            privacySettingsButton.onClick.RemoveListener(OnPrivacySettingsClicked);
    }
}
