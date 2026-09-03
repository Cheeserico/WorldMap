using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Result画面の広告呼び出し場所を集約する。
/// ResultPopupと同じGameObjectに追加し、ボタンのOnClickをこのクラスに登録する。
/// 読み込み・表示間隔・広告表示中の停止処理はInterstitialAdControllerに任せる。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ResultPopup))]
public sealed class ResultAdActions : MonoBehaviour
{
    [Header("広告を確認するボタン")]
    [SerializeField] private bool showAdOnRetry = true;
    [SerializeField] private bool showAdOnTitle = true;

    private ResultPopup popup;
    private bool exitRequested;
    private bool transitionStarted;
    private int requestVersion;

    private void Awake()
    {
        popup = GetComponent<ResultPopup>();
    }

    private void OnEnable()
    {
        ++requestVersion;
        exitRequested = false;
        transitionStarted = false;
        popup.SetInputLocked(false);
    }

    private void OnDisable()
    {
        // Popupが別経路で閉じられた場合、古い広告コールバックで移動しない。
        ++requestVersion;
        exitRequested = false;
        transitionStarted = false;
        if (popup != null) popup.SetInputLocked(false);
    }

    public void OnRetryButton()
    {
        RequestExit(true, showAdOnRetry);
    }

    public void OnTitleButton()
    {
        RequestExit(false, showAdOnTitle);
    }

    private void RequestExit(bool retry, bool checkAd)
    {
        if (!isActiveAndEnabled || exitRequested || transitionStarted ||
            popup == null || !popup.isActiveAndEnabled) return;

        ResultManager resultManager = ResultManager.Instance;
        if (resultManager == null)
        {
            Debug.LogError("ResultAdActions：ResultManagerが見つかりません。", this);
            return;
        }

        InterstitialAdController controller = null;
        if (AdManager.Instance != null)
            controller = AdManager.Instance.GetComponent<InterstitialAdController>();
        if (controller != null && controller.IsShowing) return;

        exitRequested = true;
        popup.SetInputLocked(true);
        int version = ++requestVersion;
        int sourceSceneHandle = gameObject.scene.handle;
        Debug.Log("[Ads Result] " + (retry ? "リトライ" : "タイトル") +
            (checkAd ? "：広告を確認します。" : "：広告なしで移動します。"), this);

        System.Action continueExit = () =>
        {
            if (this == null || !isActiveAndEnabled || version != requestVersion ||
                transitionStarted || popup == null || !popup.isActiveAndEnabled) return;

            if (SceneManager.GetActiveScene().handle != sourceSceneHandle ||
                resultManager == null || ResultManager.Instance != resultManager)
            {
                Unlock();
                Debug.LogWarning("[Ads Result] シーンまたはManagerが変わったため移動を中止しました。", this);
                return;
            }

            transitionStarted = true;
            Debug.Log("[Ads Result] " + (retry ? "リトライへ移動。" : "タイトルへ移動。"), this);
            // 広告終了後（または広告をスキップしたとき）に一度だけ実行。
            popup.Close();
            if (retry) resultManager.Retry();
            else resultManager.GoToTitle();
        };

        if (!checkAd || controller == null || !controller.isActiveAndEnabled)
        {
            continueExit();
            return;
        }

        if (!controller.ShowAtResultExit(continueExit)) Unlock();
    }

    private void Unlock()
    {
        exitRequested = false;
        if (popup != null) popup.SetInputLocked(false);
    }
}
