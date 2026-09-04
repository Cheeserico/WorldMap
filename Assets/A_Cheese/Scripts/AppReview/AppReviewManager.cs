using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using Google.Play.Review;
#endif

/// <summary>
/// ステージクリア回数を記録し、条件を満たしたときに
/// iOS / Android標準のアプリ内レビューを要求する。
/// </summary>
public class AppReviewManager : MonoBehaviour
{
    public static AppReviewManager Instance { get; private set; }

    [Header("レビュー表示条件")]
    [Tooltip("何回クリアしたらレビューを要求するか")]
    [SerializeField]
    [Min(1)]
    private int requiredClearCount = 3;

    [Tooltip("クリア後、レビューを要求するまでの待ち時間")]
    [SerializeField]
    [Min(0f)]
    private float requestDelay = 2.5f;

    private const string ClearCountKey =
        "AppReview_ClearCount";

    private const string RequestedVersionKey =
        "AppReview_RequestedVersion";

    private bool isRequesting;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    /// <summary>
    /// ステージをクリアしたときに1回だけ呼ぶ。
    /// </summary>
    public void NotifyStageCleared()
    {
        int clearCount =
            PlayerPrefs.GetInt(ClearCountKey, 0) + 1;

        PlayerPrefs.SetInt(ClearCountKey, clearCount);
        PlayerPrefs.Save();

        Debug.Log(
            $"[App Review] クリア回数：{clearCount} / " +
            $"{requiredClearCount}",
            this
        );

        if (clearCount < requiredClearCount)
        {
            return;
        }

        if (WasRequestedInCurrentVersion())
        {
            Debug.Log(
                "[App Review] このバージョンでは要求済みです。",
                this
            );

            return;
        }

        if (isRequesting)
        {
            return;
        }

        StartCoroutine(RequestReviewAfterDelay());
    }


    private IEnumerator RequestReviewAfterDelay()
    {
        isRequesting = true;

        if (requestDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                requestDelay
            );
        }

#if UNITY_IOS

        bool requestStarted =
            UnityEngine.iOS.Device.RequestStoreReview();

        if (requestStarted)
        {
            MarkRequestedInCurrentVersion();
            Debug.Log("[App Review] iOSへレビューを要求しました。", this);
        }
        else
        {
            Debug.LogWarning(
                "[App Review] iOSのレビュー要求を開始できませんでした。",
                this
            );
        }

#elif UNITY_ANDROID

        ReviewManager reviewManager =
            new ReviewManager();

        var requestOperation =
            reviewManager.RequestReviewFlow();

        yield return requestOperation;

        if (requestOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning(
                "[App Review] レビュー情報の取得に失敗：" +
                requestOperation.Error,
                this
            );

            isRequesting = false;
            yield break;
        }

        PlayReviewInfo reviewInfo =
            requestOperation.GetResult();

        var launchOperation =
            reviewManager.LaunchReviewFlow(reviewInfo);

        yield return launchOperation;

        reviewInfo = null;

        if (launchOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning(
                "[App Review] レビュー画面の開始に失敗：" +
                launchOperation.Error,
                this
            );

            isRequesting = false;
            yield break;
        }

        MarkRequestedInCurrentVersion();
        Debug.Log("[App Review] Androidへレビューを要求しました。", this);

#else

        Debug.Log(
            "[App Review] Unity Editorではレビュー画面を表示しません。",
            this
        );

#endif

        isRequesting = false;
    }


    private bool WasRequestedInCurrentVersion()
    {
        string requestedVersion =
            PlayerPrefs.GetString(
                RequestedVersionKey,
                string.Empty
            );

        return requestedVersion == Application.version;
    }


    private void MarkRequestedInCurrentVersion()
    {
        PlayerPrefs.SetString(
            RequestedVersionKey,
            Application.version
        );

        PlayerPrefs.Save();
    }


    [ContextMenu("App Review / Reset Test Data")]
    private void ResetTestData()
    {
        StopAllCoroutines();
        isRequesting = false;

        PlayerPrefs.DeleteKey(ClearCountKey);
        PlayerPrefs.DeleteKey(RequestedVersionKey);
        PlayerPrefs.Save();

        Debug.Log(
            "[App Review] テストデータをリセットしました。",
            this
        );
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
