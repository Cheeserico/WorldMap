using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム画面・ポーズ画面のホーム広告の呼び出しを集約する。
/// PauseManagerと同じ常時有効なGameObjectに1つだけ付ける。
/// Popupやボタン自体には付けない。OSホーム/アプリ終了時には呼ばない。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PauseManager))]
public sealed class HomeAdActions : MonoBehaviour
{
    [Header("広告を確認する場所")]
    [SerializeField] private bool showAdOnGameHome = true;
    [SerializeField] private bool showAdOnPauseHome = true;
    private PauseManager pauseManager;
    private bool pending;
    private bool completed;
    private int version;

    private void Awake() => pauseManager = GetComponent<PauseManager>();

    // ゲーム画面のホームボタンのOnClick
    public void OnGameHomeButton() => RequestHome(showAdOnGameHome, "ゲーム画面ホーム");

    // ポーズ画面のホームボタンのOnClick
    public void OnPauseHomeButton() => RequestHome(showAdOnPauseHome, "ポーズ画面ホーム");

    private void RequestHome(bool useAd, string placement)
    {
        if (!isActiveAndEnabled || pending || completed || pauseManager == null ||
            !pauseManager.isActiveAndEnabled) return;

        InterstitialAdController controller = AdManager.Instance == null ? null :
            AdManager.Instance.GetComponent<InterstitialAdController>();
        if (controller != null && controller.IsShowing) return;
        if (!pauseManager.TryReserveHomeTransition(this)) return;

        pending = true;
        int request = ++version;
        int sourceScene = gameObject.scene.handle;
        Debug.Log("[Ads Home] " + placement + (useAd ? "：広告を確認します。" : "：広告なしで戻ります。"), this);

        System.Action next = () =>
        {
            if (this == null || request != version || !pending || completed) return;
            if (!isActiveAndEnabled || pauseManager == null || !pauseManager.isActiveAndEnabled ||
                SceneManager.GetActiveScene().handle != sourceScene)
            {
                CancelRequest();
                return;
            }
            pending = false;
            completed = true;
            Debug.Log("[Ads Home] " + placement + "：途中保存してタイトルへ移動。", this);
            pauseManager.CompleteHomeTransition(this);
        };

        if (!useAd || controller == null || !controller.isActiveAndEnabled)
        {
            next();
            return;
        }
        if (!controller.ShowAtTransition(next)) CancelRequest();
    }

    private void CancelRequest()
    {
        ++version;
        pending = false;
        if (pauseManager != null) pauseManager.CancelHomeTransition(this);
    }

    private void OnDisable() => CancelRequest();
}
