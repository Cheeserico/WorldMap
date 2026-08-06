using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// すべてのPopupが継承する共通クラス。
///
/// ・Popupの表示
/// ・Popupの非表示
/// ・フェード
/// ・拡大縮小アニメーション
/// ・開閉中の入力制御
/// ・二重操作防止
///
/// を担当する。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PopupBase : MonoBehaviour
{
    [Header("共通参照")]

    [Tooltip("Popup全体に付いているCanvasGroup")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [Tooltip("拡大縮小させるPopupWindow")]
    [SerializeField]
    private RectTransform popupWindow;

    [Header("表示アニメーション")]

    [Tooltip("Popupを開く時間")]
    [SerializeField]
    [Min(0f)]
    private float openDuration = 0.2f;

    [Tooltip("Popupを閉じる時間")]
    [SerializeField]
    [Min(0f)]
    private float closeDuration = 0.15f;

    [Tooltip("表示開始時と非表示終了時の大きさ")]
    [SerializeField]
    [Range(0.1f, 1f)]
    private float hiddenScale = 0.85f;

    [SerializeField]
    private Ease openEase = Ease.OutBack;

    [SerializeField]
    private Ease closeEase = Ease.InBack;

    // 現在実行中のアニメーション
    private Sequence currentSequence;

    // 現在Popupが表示されているか
    private bool isOpen;

    // 開閉アニメーション中か
    private bool isTransitioning;

    /// <summary>
    /// Popupが現在開いているか。
    /// </summary>
    public bool IsOpen => isOpen;

    /// <summary>
    /// Popupが開閉アニメーション中か。
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    /// <summary>
    /// Popupが完全に開いたときに呼ばれる。
    /// </summary>
    public event Action Opened;

    /// <summary>
    /// Popupが完全に閉じたときに呼ばれる。
    /// </summary>
    public event Action Closed;

    /// <summary>
    /// コンポーネント追加時やReset時に、
    /// 参照を自動設定する。
    /// </summary>
    protected virtual void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (popupWindow == null)
        {
            Transform windowTransform =
                transform.Find("PopupWindow");

            if (windowTransform != null)
            {
                popupWindow =
                    windowTransform as RectTransform;
            }
        }
    }

    protected virtual void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (popupWindow == null)
        {
            Debug.LogWarning(
                $"{name}：PopupWindowが設定されていません。",
                this
            );
        }

        InitializeHiddenState();
    }

    /// <summary>
    /// Popupの初期状態を非表示にする。
    ///
    /// GameObject自体はONのままにし、
    /// CanvasGroupで見えない・触れない状態にする。
    /// </summary>
    private void InitializeHiddenState()
    {
        KillCurrentSequence();

        isOpen = false;
        isTransitioning = false;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (popupWindow != null)
        {
            popupWindow.localScale =
                Vector3.one * hiddenScale;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Popupを表示する。
    ///
    /// PopupManagerから呼び出す想定。
    /// </summary>
    public virtual void Open()
    {
        if (isOpen || isTransitioning)
        {
            return;
        }

        KillCurrentSequence();

        gameObject.SetActive(true);

        isTransitioning = true;
        isOpen = false;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        if (popupWindow != null)
        {
            popupWindow.localScale =
                Vector3.one * hiddenScale;
        }

        OnBeforeOpen();

        currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        currentSequence.Append(
            canvasGroup
                .DOFade(1f, openDuration)
                .SetEase(Ease.OutQuad)
        );

        if (popupWindow != null)
        {
            currentSequence.Join(
                popupWindow
                    .DOScale(
                        Vector3.one,
                        openDuration
                    )
                    .SetEase(openEase)
            );
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            isOpen = true;
            isTransitioning = false;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (popupWindow != null)
            {
                popupWindow.localScale =
                    Vector3.one;
            }

            OnAfterOpen();
            Opened?.Invoke();
        });
    }

    /// <summary>
    /// Popupを非表示にする。
    ///
    /// PopupManagerまたはPopup内のボタンから呼び出す。
    /// </summary>
    public virtual void Close()
    {
        if (!isOpen || isTransitioning)
        {
            return;
        }

        KillCurrentSequence();

        isTransitioning = true;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        OnBeforeClose();

        currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        currentSequence.Append(
            canvasGroup
                .DOFade(0f, closeDuration)
                .SetEase(Ease.InQuad)
        );

        if (popupWindow != null)
        {
            currentSequence.Join(
                popupWindow
                    .DOScale(
                        Vector3.one * hiddenScale,
                        closeDuration
                    )
                    .SetEase(closeEase)
            );
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            isOpen = false;
            isTransitioning = false;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            OnAfterClose();
            Closed?.Invoke();

            gameObject.SetActive(false);
        });
    }

    /// <summary>
    /// アニメーションなしで即座に表示する。
    ///
    /// 基本的には使わないが、
    /// デバッグや特殊な画面遷移用に用意。
    /// </summary>
    public virtual void OpenImmediate()
    {
        KillCurrentSequence();

        gameObject.SetActive(true);

        isOpen = true;
        isTransitioning = false;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (popupWindow != null)
        {
            popupWindow.localScale =
                Vector3.one;
        }

        OnBeforeOpen();
        OnAfterOpen();
        Opened?.Invoke();
    }

    /// <summary>
    /// アニメーションなしで即座に非表示にする。
    /// </summary>
    public virtual void CloseImmediate()
    {
        KillCurrentSequence();

        isOpen = false;
        isTransitioning = false;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (popupWindow != null)
        {
            popupWindow.localScale =
                Vector3.one * hiddenScale;
        }

        OnBeforeClose();
        OnAfterClose();
        Closed?.Invoke();

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Popupを開く直前に呼ばれる。
    ///
    /// PausePopupUIやResultPopupUI側で
    /// 必要に応じてoverrideする。
    /// </summary>
    protected virtual void OnBeforeOpen()
    {
    }

    /// <summary>
    /// Popupが完全に開いた後に呼ばれる。
    /// </summary>
    protected virtual void OnAfterOpen()
    {
    }

    /// <summary>
    /// Popupを閉じる直前に呼ばれる。
    /// </summary>
    protected virtual void OnBeforeClose()
    {
    }

    /// <summary>
    /// Popupが完全に閉じた後に呼ばれる。
    /// </summary>
    protected virtual void OnAfterClose()
    {
    }

    /// <summary>
    /// 実行中のDOTweenを停止する。
    /// </summary>
    private void KillCurrentSequence()
    {
        if (currentSequence != null &&
            currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        currentSequence = null;
    }

    protected virtual void OnDestroy()
    {
        KillCurrentSequence();

        Opened = null;
        Closed = null;
    }
}