using System;
using System.Collections.Concurrent;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 工程1: タイトルでの同意確認とSDK初期化。広告の生成・表示は行わない。
/// 設定画面からプライバシーの選択を変更できる。広告の生成・表示はまだ行わない。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AdAudienceSettings))]
public sealed class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Tooltip("Generalで同意可能年齢未満と判断した場合に使用。年齢確認機能ではありません。")]
    [SerializeField] private bool userUnderAgeOfConsent;
    [Min(5f)][SerializeField] private float requestTimeoutSeconds = 20f;

    private enum Phase { Idle, UpdatingConsent, LoadingForm, ShowingForm, Initializing, Ready, Unavailable }
    [SerializeField] private Phase phase = Phase.Idle;
    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    private int titleSceneHandle;
    private float deadline;
    private bool sdkInitialized;
    private bool sdkInitializationStarted;
    private bool ownsInstance;
    private bool privacyOptionsShowing;

    // 広告を実際にリクエストする直前にも、この値を確認すること。
    public bool CanRequestAds => !privacyOptionsShowing && sdkInitialized && phase == Phase.Ready && ConsentInformation.CanRequestAds();
    // UIはこの保存値を読む。毎フレームUMPへ問い合わせない。
    // 広告可否のCanRequestAdsは、引き続きSDKでその都度確認する。
    public bool PrivacyOptionsRequired { get; private set; }

    private void RefreshPrivacyOptionsRequirement()
    {
        PrivacyOptionsRequired =
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
    }

    // SDK初期化待ちや、別の同意画面の表示中は重ねて開かない。
    public bool CanShowPrivacyOptions => ownsInstance && isActiveAndEnabled &&
        PrivacyOptionsRequired && !privacyOptionsShowing &&
        (phase == Phase.Ready || phase == Phase.Unavailable);

    /// <summary>
    /// 設定画面のボタンからのみ呼ぶ。自動では表示しない。
    /// PausePopupの一時停止状態や音量設定は変更しない。
    /// </summary>
    public void ShowPrivacyOptions()
    {
        if (!CanShowPrivacyOptions) return;
        privacyOptionsShowing = true;
        try
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
                mainThreadActions.Enqueue(() => FinishPrivacyOptions(error)));
        }
        catch (Exception error)
        {
            privacyOptionsShowing = false;
            Debug.LogWarning("[Ads] プライバシー設定を開けませんでした: " + error.Message, this);
            InitializeIfAllowed();
        }
    }

    private void FinishPrivacyOptions(FormError error)
    {
        if (!privacyOptionsShowing) return;
        privacyOptionsShowing = false;
        if (error != null)
            Debug.LogWarning("[Ads] プライバシー設定の表示エラー: " + error.Message, this);
        else
            Debug.Log("[Ads] プライバシー設定を閉じました。広告の可否を再確認します。", this);
        InitializeIfAllowed();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (transform.parent != null)
        {
            Debug.LogError("[Ads] AdSystemはHierarchyの最上位に配置してください。", this);
            enabled = false;
            return;
        }
        Instance = this;
        ownsInstance = true;
        titleSceneHandle = gameObject.scene.handle;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!ownsInstance) return;
        SetPhase(Phase.UpdatingConsent);
        try
        {
            var parameters = GetComponent<AdAudienceSettings>().PrepareForConsent(userUnderAgeOfConsent);
            ConsentInformation.Update(parameters, error => mainThreadActions.Enqueue(() => OnConsentUpdated(error)));
        }
        catch (Exception error) { Fail(error.Message); }
    }

    private void Update()
    {
        if (!ownsInstance) return;

        // 同意確認/読み込みが遅れても、タイトルを離れた後にはフォームを出さない。
        if ((phase == Phase.UpdatingConsent || phase == Phase.LoadingForm) && !IsTitleActive())
            Fail("タイトルを離れたため、今回の同意確認を中止しました。");

        if ((phase == Phase.UpdatingConsent || phase == Phase.LoadingForm || phase == Phase.Initializing)
            && Time.realtimeSinceStartup >= deadline)
            Fail("通信または初期化がタイムアウトしました。ゲームは続行できます。");

        while (mainThreadActions.TryDequeue(out Action action))
        {
            try { action(); }
            catch (Exception error) { Fail(error.Message); }
        }
    }

    private bool IsTitleActive() => SceneManager.GetActiveScene().handle == titleSceneHandle;

    private void OnConsentUpdated(FormError error)
    {
        // タイトルを離れていても、完了した更新のボタン表示条件は反映する。
        RefreshPrivacyOptionsRequirement();
        if (phase != Phase.UpdatingConsent) return;
        if (error != null)
        {
            Debug.LogWarning("[Ads] 同意情報の更新に失敗: " + error.Message, this);
            // UMPが前回の状態でリクエスト可能と判断する場合のみ先へ進める。
            InitializeIfAllowed();
            return;
        }
        if (ConsentInformation.ConsentStatus != ConsentStatus.Required)
        {
            InitializeIfAllowed();
            return;
        }

        SetPhase(Phase.LoadingForm);
        ConsentForm.Load((form, loadError) => mainThreadActions.Enqueue(() =>
        {
            if (phase != Phase.LoadingForm || !IsTitleActive()) return;
            if (loadError != null || form == null)
            {
                Debug.LogWarning("[Ads] 同意画面を読み込めませんでした: " + loadError?.Message, this);
                InitializeIfAllowed();
                return;
            }
            SetPhase(Phase.ShowingForm);
            // ユーザーの回答待ちには通信タイムアウトを適用しない。
            form.Show(showError => mainThreadActions.Enqueue(() =>
            {
                if (phase != Phase.ShowingForm) return;
                if (showError != null)
                    Debug.LogWarning("[Ads] 同意画面のエラー: " + showError.Message, this);
                InitializeIfAllowed();
            }));
        }));
    }

    private void InitializeIfAllowed()
    {
        // 初回フォーム・設定フォームを閉じた後や、表示失敗時に更新する。
        RefreshPrivacyOptionsRequirement();
        if (!ConsentInformation.CanRequestAds())
        {
            Fail("現在は広告をリクエストできません。ゲームは続行できます。");
            return;
        }
        if (sdkInitialized)
        {
            SetPhase(Phase.Ready);
            return;
        }
        // タイムアウト後も、開始済みのSDK初期化を重ねて呼ばない。
        if (sdkInitializationStarted) return;
        sdkInitializationStarted = true;
        SetPhase(Phase.Initializing);
        MobileAds.Initialize(status => mainThreadActions.Enqueue(() =>
        {
            // 初期化がタイムアウト後に完了した場合も、重複初期化せず復帰できる。
            if (status == null) { Fail("SDK初期化結果を取得できませんでした。"); return; }
            sdkInitialized = true;
            SetPhase(ConsentInformation.CanRequestAds() ? Phase.Ready : Phase.Unavailable);
            Debug.Log("[Ads] SDK初期化完了。広告表示はまだ行いません。", this);
        }));
    }

    private void SetPhase(Phase next)
    {
        phase = next;
        deadline = Time.realtimeSinceStartup + Mathf.Max(5f, requestTimeoutSeconds);
        Debug.Log("[Ads] " + next, this);
    }

    private void Fail(string message)
    {
        phase = Phase.Unavailable;
        Debug.LogWarning("[Ads] " + message, this);
    }

    private void OnDestroy()
    {
        if (ownsInstance && Instance == this) Instance = null;
    }
}
