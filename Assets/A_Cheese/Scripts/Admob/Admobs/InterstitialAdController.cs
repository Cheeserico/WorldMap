using System;
using System.Threading;
using GoogleMobileAds.Api;
using UnityEngine;

/// <summary>
/// AdSystemに追加する。SDK初期化と同意判断はAdManagerに任せる。
/// 読み込みだけでは表示しない。画面遷移の区切りでShowAtTransitionを呼ぶ。
/// 呼び出し側はボタン連打を防止し、完了コールバック内でのみ遷移すること。
/// 表示中はAdSystemを無効化・破棄しないこと。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AdManager))]
public sealed class InterstitialAdController : MonoBehaviour
{
    [Header("インタースティシャル広告")]
    [SerializeField] private bool enableInterstitial = true;
    [Tooltip("Google公式テスト広告ユニットID。端末IDではありません。")]
    [SerializeField] private string androidAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    [SerializeField] private string iosAdUnitId = "ca-app-pub-3940256099942544/4411468910";
    [Header("表示間隔（秒・起動直後にも適用）")]
    [SerializeField, Min(0f)] private float minimumIntervalSeconds = 120f;

    private AdManager manager;
    private SynchronizationContext unityContext;
    private InterstitialAd cachedAd;
    private InterstitialAd showingAd;
    private Action continuation;
    private int loadGeneration;
    private bool loading;
    private bool sessionActive;
    private float loadStartedAt;
    private float loadedAt;
    private float nextLoadAt;
    private float lastShowFinishedAt;
    private bool previousAudioPause;
    private float previousTimeScale;
    public bool IsShowing => showingAd != null;

    private bool Allowed => sessionActive && enableInterstitial && manager != null &&
        AdManager.Instance == manager && manager.CanRequestAds;

    private void Awake()
    {
        unityContext = SynchronizationContext.Current;
        manager = GetComponent<AdManager>();
        lastShowFinishedAt = Time.realtimeSinceStartup;
    }

    private void OnEnable()
    {
        sessionActive = true;
        manager.AdvertisingAvailabilityChanged += OnAvailabilityChanged;
        OnAvailabilityChanged(manager.AdvertisingAvailable);
    }

    private void OnDisable()
    {
        sessionActive = false;
        if (manager != null) manager.AdvertisingAvailabilityChanged -= OnAvailabilityChanged;
        InvalidateCache();
        // 実際の広告を閉じるイベントまでは遷移させない。
        // SynchronizationContextにより無効化中も完了通知を受け取れる。
    }

    private void OnAvailabilityChanged(bool available)
    {
        if (!available) InvalidateCache();
        else TryPreload();
    }

    private void Update()
    {
        float now = Time.realtimeSinceStartup;
        if (!enableInterstitial && (loading || cachedAd != null)) InvalidateCache();
        if (loading && now - loadStartedAt >= 30f)
        {
            InvalidateCache();
            nextLoadAt = now + 60f;
            Debug.LogWarning("[Ads Interstitial] 読み込みタイムアウト。ゲームは続行できます。", this);
        }
        if (cachedAd != null && now - loadedAt >= 3300f) InvalidateCache();
        TryPreload();
    }

    private void TryPreload()
    {
        // 毎フレームUMPへ問い合わせず、通知の保存値で先に絞る。
        if (!sessionActive || !enableInterstitial || manager == null ||
            !manager.AdvertisingAvailable || loading || cachedAd != null || IsShowing ||
            Time.realtimeSinceStartup < nextLoadAt) return;
        if (!Allowed || unityContext == null) return;
#if UNITY_ANDROID || UNITY_EDITOR
        string unitId = androidAdUnitId;
#elif UNITY_IOS
        string unitId = iosAdUnitId;
#else
        return;
#endif
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(unitId)) return;
        loading = true;
        loadStartedAt = Time.realtimeSinceStartup;
        int generation = ++loadGeneration;
        var context = unityContext;
        Debug.Log("[Ads Interstitial] 読み込み開始。", this);
        try
        {
            InterstitialAd.Load(unitId, new AdRequest(), (ad, error) =>
                context.Post(_ =>
                {
                    if (this == null || !sessionActive || generation != loadGeneration)
                    {
                        DestroyAd(ad);
                        return;
                    }
                    loading = false;
                    if (error != null || ad == null || !Allowed)
                    {
                        DestroyAd(ad);
                        nextLoadAt = Time.realtimeSinceStartup + 60f;
                        Debug.LogWarning("[Ads Interstitial] 読み込みを完了できませんでした: " + error, this);
                        return;
                    }
                    cachedAd = ad;
                    loadedAt = Time.realtimeSinceStartup;
                    Debug.Log("[Ads Interstitial] 読み込み完了。まだ表示しません。", this);
                }, null));
        }
        catch (Exception error)
        {
            loading = false;
            nextLoadAt = Time.realtimeSinceStartup + 60f;
            Debug.LogWarning("[Ads Interstitial] " + error.Message, this);
        }
#endif
    }

    /// <summary>
    /// ResultAdActionsとの互換用。共通の画面遷移処理に委譲する。
    /// </summary>
    public bool ShowAtResultExit(Action onContinue)
    {
        return ShowAtTransition(onContinue);
    }

    /// <summary>
    /// メインスレッドから、ユーザーが操作して画面を離れる区切りで呼ぶ。
    /// 未準備・広告不可・間隔不足なら即座にonContinueを実行し、後から表示しない。
    /// 表示中の二重呼び出しはfalseを返し、追加のコールバックは実行しない。
    /// 戻り値は広告表示の成否ではなく、この遷移要求を受け付けたかどうか。
    /// </summary>
    public bool ShowAtTransition(Action onContinue)
    {
        if (onContinue == null) throw new ArgumentNullException(nameof(onContinue));
        if (IsShowing) return false;

        bool ready = false;
        try
        {
            ready = Allowed && cachedAd != null &&
                Time.realtimeSinceStartup - loadedAt < 3300f &&
                Time.realtimeSinceStartup - lastShowFinishedAt >= Mathf.Max(0f, minimumIntervalSeconds) &&
                cachedAd.CanShowAd();
        }
        catch (Exception error) { Debug.LogWarning("[Ads Interstitial] " + error.Message, this); }

        if (!ready)
        {
            onContinue();
            return true;
        }

        showingAd = cachedAd;
        cachedAd = null;
        continuation = onContinue;
        var ad = showingAd;
        var context = unityContext;
        previousAudioPause = AudioListener.pause;
        previousTimeScale = Time.timeScale;
        AudioListener.pause = true;
        Time.timeScale = 0f;
        try
        {
            ad.OnAdFullScreenContentClosed += () => context.Post(_ =>
            {
                if (this != null) FinishShow(ad, null);
            }, null);
            ad.OnAdFullScreenContentFailed += error => context.Post(_ =>
            {
                if (this != null) FinishShow(ad, error.ToString());
            }, null);
            Debug.Log("[Ads Interstitial] 表示開始。", this);
            ad.Show();
        }
        catch (Exception error) { FinishShow(ad, error.Message); }
        return true;
    }

    private void FinishShow(InterstitialAd ad, string error)
    {
        if (!ReferenceEquals(showingAd, ad)) return;
        showingAd = null;
        lastShowFinishedAt = Time.realtimeSinceStartup;
        AudioListener.pause = previousAudioPause;
        Time.timeScale = previousTimeScale;
        DestroyAd(ad);
        Action next = continuation;
        continuation = null;
        if (error != null) Debug.LogWarning("[Ads Interstitial] 表示失敗: " + error, this);
        else Debug.Log("[Ads Interstitial] 閉じました。画面遷移を続行します。", this);
        // シーン移動はここだけ。表示失敗時にも一度だけ続行する。
        next?.Invoke();
    }

    private void InvalidateCache()
    {
        ++loadGeneration;
        loading = false;
        var ad = cachedAd;
        cachedAd = null;
        DestroyAd(ad);
    }

    private static void DestroyAd(InterstitialAd ad)
    {
        if (ad == null) return;
        try { ad.Destroy(); }
        catch (Exception error) { Debug.LogWarning("[Ads Interstitial] 破棄エラー: " + error.Message); }
    }

    private void OnDestroy()
    {
        InvalidateCache();
        if (IsShowing)
        {
            AudioListener.pause = previousAudioPause;
            Time.timeScale = previousTimeScale;
            DestroyAd(showingAd);
            showingAd = null;
        }
        // アプリ終了・所有者破棄時にはシーン遷移を実行しない。
        continuation = null;
    }
}
