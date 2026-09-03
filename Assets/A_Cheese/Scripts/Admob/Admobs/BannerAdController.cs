using System;
using System.Collections.Concurrent;
using GoogleMobileAds.Api;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif

/// <summary>
/// AdSystemにAdManagerと一緒に追加する、全シーン共通の上部バナー。
/// 同意取得・SDK初期化は行わず、AdManagerから利用可能状態を受け取る。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AdManager))]
public sealed class BannerAdController : MonoBehaviour
{
    [Header("全シーン共通・上部バナー")]
    [SerializeField] private bool enableBanner = true;
    [Tooltip("初期値はGoogle公式のテスト広告ユニットID。本番用は公開前に設定する。")]
    [SerializeField] private string androidBannerAdUnitId = "ca-app-pub-3940256099942544/9214589741";
    [Tooltip("初期値はGoogle公式のテスト広告ユニットID。本番用は公開前に設定する。")]
    [SerializeField] private string iosBannerAdUnitId = "ca-app-pub-3940256099942544/2435281174";

    private BannerView bannerView;
#if UNITY_EDITOR
    private bool rebuildEditorBanner;
#endif

    // UIの広告用余白を調整する際に使用できる。読み込み前は0。
    public float BannerHeightPixels { get; private set; }

    private AdManager adManager;
    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    private bool CanRequestAds => adManager != null &&
        AdManager.Instance == adManager && adManager.CanRequestAds;

    private void OnEnable()
    {
#if UNITY_EDITOR
        SceneManager.sceneLoaded += HandleEditorSceneLoaded;
#endif
        adManager = GetComponent<AdManager>();
        adManager.AdvertisingAvailabilityChanged += HandleAdvertisingAvailability;
        // 起動後にコンポーネントを有効にした場合も現在の状態を反映する。
        HandleAdvertisingAvailability(adManager.AdvertisingAvailable);
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        SceneManager.sceneLoaded -= HandleEditorSceneLoaded;
        rebuildEditorBanner = false;
#endif
        if (adManager != null)
            adManager.AdvertisingAvailabilityChanged -= HandleAdvertisingAvailability;
        DestroyBanner();
    }

    private void OnDestroy()
    {
        DestroyBanner();
    }

    private void HandleAdvertisingAvailability(bool available)
    {
        if (available && isActiveAndEnabled) LoadBannerIfAllowed();
        else DestroyBanner();
    }

#if UNITY_EDITOR
    private void HandleEditorSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Singleでのシーン切替・同一シーンの再読み込み時に仮表示を再生成する。
        // Additive読み込みは既存シーンを破棄しないので対象外。
        if (mode == LoadSceneMode.Single) rebuildEditorBanner = true;
    }
#endif

    private void Update()
    {
#if UNITY_EDITOR
        if (rebuildEditorBanner)
        {
            rebuildEditorBanner = false;
            // 古い表示への通知を無効にしてから、新しいシーンで読み込み直す。
            // Time.timeScaleが0でも動く。実機ビルドにはこの処理を含めない。
            DestroyBanner();
            LoadBannerIfAllowed();
        }
#endif
        // コールバックをメインスレッドで処理する。毎フレームUMPへは問い合わせない。
        while (mainThreadActions.TryDequeue(out Action action))
        {
            try { action(); }
            catch (Exception error)
            {
                DestroyBanner();
                Debug.LogWarning("[Ads] バナー処理に失敗: " + error.Message, this);
            }
        }
    }

    private string GetBannerAdUnitId()
    {
#if UNITY_IOS
        return iosBannerAdUnitId;
#elif UNITY_ANDROID || UNITY_EDITOR
        return androidBannerAdUnitId;
#else
        return null;
#endif
    }

    /// <summary>
    /// SDK初期化後、同意変更後に呼ぶ。実機ではシーンを移動しても同じバナーを使う。
    /// 広告失敗時に連続で再リクエストしない。自動更新はSDK側の設定に従う。
    /// </summary>
    private void LoadBannerIfAllowed()
    {
        if (!enableBanner || !CanRequestAds)
        {
            DestroyBanner();
            return;
        }
        if (bannerView != null) return;

        string adUnitId = GetBannerAdUnitId();
        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            Debug.LogWarning("[Ads] このプラットフォームのバナー広告ユニットIDが未設定です。", this);
            return;
        }

        try
        {
            int safeWidth = MobileAds.Utils.GetDeviceSafeWidth();
            AdSize size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(safeWidth);
            var createdBanner = new BannerView(adUnitId.Trim(), size, AdPosition.Top);
            bannerView = createdBanner;

            createdBanner.OnBannerAdLoaded += () => mainThreadActions.Enqueue(() =>
            {
                // 同意設定を開いて破棄した広告の、遅れて届いた通知は無視する。
                if (!ReferenceEquals(bannerView, createdBanner)) return;
                if (!enableBanner || !CanRequestAds)
                {
                    DestroyBanner();
                    return;
                }
                BannerHeightPixels = createdBanner.GetHeightInPixels();
                createdBanner.Show();
                Debug.Log("[Ads] バナー読み込み完了。高さ: " + BannerHeightPixels + " px", this);
            });

            createdBanner.OnBannerAdLoadFailed += error => mainThreadActions.Enqueue(() =>
            {
                if (!ReferenceEquals(bannerView, createdBanner)) return;
                Debug.LogWarning("[Ads] バナー読み込み失敗: " + error, this);
                // 読み込み失敗はゲーム進行や同意設定を停止させない。
            });

            createdBanner.LoadAd(new AdRequest());
            Debug.Log("[Ads] バナー広告をリクエストしました。", this);
        }
        catch (Exception error)
        {
            DestroyBanner();
            Debug.LogWarning("[Ads] バナーを作成できませんでした: " + error.Message, this);
        }
    }

    private void DestroyBanner()
    {
        BannerView previous = bannerView;
        bannerView = null;
        BannerHeightPixels = 0f;
        if (previous == null) return;
        try { previous.Destroy(); }
        catch (Exception error)
        {
            Debug.LogWarning("[Ads] バナーの終了処理に失敗: " + error.Message, this);
        }
    }

}
