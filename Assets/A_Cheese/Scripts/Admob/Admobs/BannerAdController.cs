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
    [SerializeField]
    private string androidBannerAdUnitId =
        "ca-app-pub-3940256099942544/9214589741";

    [Tooltip("初期値はGoogle公式のテスト広告ユニットID。本番用は公開前に設定する。")]
    [SerializeField]
    private string iosBannerAdUnitId =
        "ca-app-pub-3940256099942544/2435281174";

    private BannerView bannerView;

#if UNITY_EDITOR
    private bool rebuildEditorBanner;
#endif

    /// <summary>
    /// Adaptive Bannerの予定高さ（画面ピクセル）。
    /// 広告の読み込み開始前に設定し、広告更新では変更しない。
    /// </summary>
    public float BannerHeightPixels { get; private set; }

    private AdManager adManager;

    private readonly ConcurrentQueue<Action> mainThreadActions =
        new ConcurrentQueue<Action>();

    private bool CanRequestAds =>
        adManager != null &&
        AdManager.Instance == adManager &&
        adManager.CanRequestAds;


    private void OnEnable()
    {
#if UNITY_EDITOR
        SceneManager.sceneLoaded += HandleEditorSceneLoaded;
#endif

        adManager = GetComponent<AdManager>();
        adManager.AdvertisingAvailabilityChanged +=
            HandleAdvertisingAvailability;

        // UMPの完了や広告読み込みを待たず、最初にUI用の高さを予約する。
        UpdateReservedBannerHeight();

        HandleAdvertisingAvailability(
            adManager.AdvertisingAvailable
        );
    }


    private void OnDisable()
    {
#if UNITY_EDITOR
        SceneManager.sceneLoaded -= HandleEditorSceneLoaded;
        rebuildEditorBanner = false;
#endif

        if (adManager != null)
        {
            adManager.AdvertisingAvailabilityChanged -=
                HandleAdvertisingAvailability;
        }

        DestroyBanner();
    }


    private void OnDestroy()
    {
        DestroyBanner();
    }


    private void HandleAdvertisingAvailability(bool available)
    {
        if (available && isActiveAndEnabled)
        {
            LoadBannerIfAllowed();
        }
        else
        {
            DestroyBanner();
        }
    }


#if UNITY_EDITOR
    private void HandleEditorSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            rebuildEditorBanner = true;
        }
    }
#endif


    private void Update()
    {
#if UNITY_EDITOR
        if (rebuildEditorBanner)
        {
            rebuildEditorBanner = false;
            DestroyBanner();
            UpdateReservedBannerHeight();
            LoadBannerIfAllowed();
        }
#endif

        while (mainThreadActions.TryDequeue(out Action action))
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                DestroyBanner();

                Debug.LogWarning(
                    "[Ads] バナー処理に失敗: " +
                    error.Message,
                    this
                );
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
    /// 現在の画面幅からAdaptive Bannerのサイズを作る。
    /// 同じ計算をUI予約とBannerView生成で共有する。
    /// </summary>
    private AdSize CreateAdaptiveAdSize()
    {
        int safeWidth =
            MobileAds.Utils.GetDeviceSafeWidth();

        return AdSize
            .GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
                safeWidth
            );
    }


    /// <summary>
    /// 広告が届く前に、予定サイズを画面ピクセルへ変換して公開する。
    /// </summary>
    private void UpdateReservedBannerHeight()
    {
        if (!enableBanner)
        {
            BannerHeightPixels = 0f;
            return;
        }

        try
        {
            float deviceScale =
                MobileAds.Utils.GetDeviceScale();

            if (deviceScale <= 0f || Screen.height <= 0)
            {
                return;
            }

            // Anchored Adaptive Bannerの公式範囲：
            // 画面高の15%を基準に、50～90dpへ制限する。
            float heightFromScreen =
                Screen.height * 0.15f;

            float minimumHeightPixels =
                50f * deviceScale;

            float maximumHeightPixels =
                90f * deviceScale;

            BannerHeightPixels = Mathf.Ceil(
                Mathf.Clamp(
                    heightFromScreen,
                    minimumHeightPixels,
                    maximumHeightPixels
                )
            );

            Debug.Log(
                "[Ads] バナー予定高さを予約: " +
                BannerHeightPixels + " px" +
                "（画面高: " + Screen.height +
                " / DeviceScale: " + deviceScale + "）",
                this
            );
        }
        catch (Exception error)
        {
            // UI側はminimumAreaHeightを使えるため、起動を止めない。
            Debug.LogWarning(
                "[Ads] バナー予定高さを取得できませんでした: " +
                error.Message,
                this
            );
        }
    }


    /// <summary>
    /// SDK初期化後、同意変更後に呼ぶ。
    /// 自動更新はSDK側の設定に従う。
    /// </summary>
    private void LoadBannerIfAllowed()
    {
        if (!enableBanner || !CanRequestAds)
        {
            DestroyBanner();
            return;
        }

        if (bannerView != null)
        {
            return;
        }

        string adUnitId = GetBannerAdUnitId();

        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            Debug.LogWarning(
                "[Ads] このプラットフォームの" +
                "バナー広告ユニットIDが未設定です。",
                this
            );

            return;
        }

        try
        {
            AdSize size = CreateAdaptiveAdSize();

            // 何らかの理由で予約できていなければ、ここでも計算する。
            if (BannerHeightPixels <= 0f)
            {
                UpdateReservedBannerHeight();
            }

            var createdBanner = new BannerView(
                adUnitId.Trim(),
                size,
                AdPosition.Top
            );

            bannerView = createdBanner;

            createdBanner.OnBannerAdLoaded += () =>
                mainThreadActions.Enqueue(() =>
                {
                    if (!ReferenceEquals(
                        bannerView,
                        createdBanner))
                    {
                        return;
                    }

                    if (!enableBanner || !CanRequestAds)
                    {
                        DestroyBanner();
                        return;
                    }

                    // 実測値は確認だけに使い、予約したUI高さは変更しない。
                    float actualHeightPixels =
                        createdBanner.GetHeightInPixels();

                    createdBanner.Show();

                    Debug.Log(
                        "[Ads] バナー読み込み完了。" +
                        "予約: " + BannerHeightPixels +
                        " px / 実測: " + actualHeightPixels +
                        " px",
                        this
                    );
                });

            createdBanner.OnBannerAdLoadFailed += error =>
                mainThreadActions.Enqueue(() =>
                {
                    if (!ReferenceEquals(
                        bannerView,
                        createdBanner))
                    {
                        return;
                    }

                    Debug.LogWarning(
                        "[Ads] バナー読み込み失敗: " +
                        error,
                        this
                    );
                });

            createdBanner.LoadAd(new AdRequest());

            Debug.Log(
                "[Ads] バナー広告をリクエストしました。",
                this
            );
        }
        catch (Exception error)
        {
            DestroyBanner();

            Debug.LogWarning(
                "[Ads] バナーを作成できませんでした: " +
                error.Message,
                this
            );
        }
    }


    private void DestroyBanner()
    {
        BannerView previous = bannerView;
        bannerView = null;

        // BannerHeightPixelsは0へ戻さない。
        // 再読み込み中も同じUI余白を維持して、画面の上下移動を防ぐ。

        if (previous == null)
        {
            return;
        }

        try
        {
            previous.Destroy();
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "[Ads] バナーの終了処理に失敗: " +
                error.Message,
                this
            );
        }
    }
}
