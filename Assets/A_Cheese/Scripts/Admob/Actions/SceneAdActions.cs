using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// このゲームシーンで広告を呼ぶ場所を集約する。
/// 常時有効なManagers等に1つだけ追加する。AdSystemやPopupには付けない。
/// ResultAdActions/HomeAdActionsとは併用しない。
/// 読み込み・同意判断・共通の表示間隔は既存の広告Controllerへ任せる。
/// </summary>
[DisallowMultipleComponent]
public sealed class SceneAdActions : MonoBehaviour
{
    [Header("同じゲームシーン内の参照（Inspectorで設定）")]
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private ResultPopup resultPopup;

    [Header("Result：広告を確認するボタン")]
    [SerializeField] private bool showAdOnResultRetry = true;
    [SerializeField] private bool showAdOnResultTitle = true;
    [Header("ゲーム途中：広告を確認するボタン")]
    [SerializeField] private bool showAdOnGameHome = true;
    [SerializeField] private bool showAdOnPauseHome = true;

    private bool pending;
    private bool transitionStarted;
    private int requestVersion;
    private PauseManager reservedPauseManager;
    private ResultPopup lockedPopup;

    public void OnResultRetryButton() => RequestResult(true, showAdOnResultRetry);
    public void OnResultTitleButton() => RequestResult(false, showAdOnResultTitle);
    public void OnGameHomeButton() => RequestHome(showAdOnGameHome, "ゲーム画面ホーム");
    public void OnPauseHomeButton() => RequestHome(showAdOnPauseHome, "ポーズ画面ホーム");

    private bool CanBegin(out InterstitialAdController controller)
    {
        controller = null;
        if (!isActiveAndEnabled || pending || transitionStarted) return false;
        if (AdManager.Instance != null)
            controller = AdManager.Instance.GetComponent<InterstitialAdController>();
        return controller == null || !controller.IsShowing;
    }

    private void RequestResult(bool retry, bool useAd)
    {
        if (!CanBegin(out InterstitialAdController controller)) return;
        ResultManager manager = ResultManager.Instance;
        ResultPopup popup = resultPopup;
        if (popup == null || manager == null)
        {
            Debug.LogError("SceneAdActions：Result Popupの参照またはResultManagerを確認してください。", this);
            return;
        }
        if (!popup.isActiveAndEnabled || popup.gameObject.scene.handle != gameObject.scene.handle) return;

        lockedPopup = popup;
        popup.SetInputLocked(true);
        // 別経路でResultが閉じられたら、古い広告の完了で移動しない。
        popup.Closed += OnResultClosed;

        Dispatch(controller, useAd, retry ? "Resultリトライ" : "Resultタイトル",
            () => popup != null && popup.isActiveAndEnabled &&
                  manager != null && ResultManager.Instance == manager,
            () =>
            {
                ReleasePopupLock();
                popup.Close();
                if (retry) manager.Retry();
                else manager.GoToTitle();
            });
    }

    private void RequestHome(bool useAd, string placement)
    {
        if (!CanBegin(out InterstitialAdController controller)) return;
        PauseManager manager = pauseManager;
        if (manager == null)
        {
            Debug.LogError("SceneAdActions：Pause ManagerをInspectorに設定してください。", this);
            return;
        }
        if (manager.gameObject.scene.handle != gameObject.scene.handle ||
            !manager.TryReserveHomeTransition(this)) return;

        reservedPauseManager = manager;
        Dispatch(controller, useAd, placement,
            () => manager != null && manager.isActiveAndEnabled,
            () =>
            {
                // 途中保存・TimeScale/BGM復元・移動はPauseManagerの既存処理。
                manager.CompleteHomeTransition(this);
                reservedPauseManager = null;
            });
    }

    private void Dispatch(InterstitialAdController controller, bool useAd,
        string placement, Func<bool> destinationStillValid, Action navigate)
    {
        pending = true;
        int version = ++requestVersion;
        int sceneHandle = gameObject.scene.handle;
        Debug.Log("[Ads Scene] " + placement +
            (useAd ? "：広告を確認します。" : "：広告なしで移動します。"), this);

        Action next = () =>
        {
            if (this == null || version != requestVersion || !pending || transitionStarted) return;
            if (!isActiveAndEnabled || SceneManager.GetActiveScene().handle != sceneHandle ||
                !destinationStillValid())
            {
                CancelPending();
                return;
            }
            pending = false;
            transitionStarted = true;
            Debug.Log("[Ads Scene] " + placement + "：移動を実行します。", this);
            try { navigate(); }
            catch (Exception error)
            {
                transitionStarted = false;
                CancelPending();
                Debug.LogException(error, this);
            }
        };

        if (!useAd || controller == null || !controller.isActiveAndEnabled)
        {
            next();
            return;
        }
        // 未準備・間隔不足は即続行。読み込みを待って後から表示しない。
        if (!controller.ShowAtTransition(next)) CancelPending();
    }

    private void OnResultClosed()
    {
        if (pending) CancelPending();
    }

    private void ReleasePopupLock()
    {
        if (lockedPopup != null)
        {
            lockedPopup.Closed -= OnResultClosed;
            lockedPopup.SetInputLocked(false);
        }
        lockedPopup = null;
    }

    private void CancelPending()
    {
        ++requestVersion;
        pending = false;
        ReleasePopupLock();
        if (reservedPauseManager != null) reservedPauseManager.CancelHomeTransition(this);
        reservedPauseManager = null;
    }

    private void OnDisable() => CancelPending();
}
