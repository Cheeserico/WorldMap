using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 国ピースのドラッグ、
/// ドラッグ中の地図実寸表示、
/// 不正解時の復帰、
/// 正解時のDOTween吸着配置を管理する。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class CountryPieceDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("このピースの国ID")]
    [SerializeField]
    private string countryId;

    [Header("ドラッグ中のピースを置く場所")]
    [SerializeField]
    private RectTransform dragLayer;

    [Header("ピース表示")]
    [SerializeField]
    private RectTransform countryImageRect;

    [SerializeField]
    private Image cardBackgroundImage;

    [SerializeField]
    private TMP_Text countryNameText;

    [Header("地図へ配置した後の設定")]
    [SerializeField]
    private Vector3 placedLocalScale = Vector3.one;

    [SerializeField]
    private float placedRotationZ = 0f;

    [Header("正解時のDOTween演出")]
    [SerializeField]
    private float snapDuration = 0.25f;

    [SerializeField]
    private Ease snapEase = Ease.OutCubic;

    [SerializeField]
    private float punchScaleAmount = 0.1f;

    [SerializeField]
    private float punchDuration = 0.2f;

    [Header("不正解時のDOTween演出")]
    [SerializeField]
    private float returnDuration = 0.25f;

    [SerializeField]
    private Ease returnEase = Ease.OutCubic;

    [SerializeField]
    private float wrongShakeStrength = 12f;

    [SerializeField]
    private float wrongShakeDuration = 0.18f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // ドラッグ開始前の親
    private Transform originalParent;

    // ドラッグ開始前のHierarchy上の並び順
    private int originalSiblingIndex;

    // ドラッグ開始前の位置
    private Vector2 originalAnchoredPosition;

    // ドラッグ開始前の大きさ
    private Vector2 originalSizeDelta;

    // Country_Imageの元サイズ
    private Vector2 originalCountryImageSize;

    // ドラッグ開始前のScale
    private Vector3 originalLocalScale;

    // ドラッグ開始前の角度
    private Quaternion originalLocalRotation;

    // カード背景の元状態
    private bool originalCardBackgroundEnabled;

    // 国名の元状態
    private bool originalCountryNameActive;

    // 今回のドラッグで正解したか
    private bool wasPlacedSuccessfully;

    // すでに地図へ配置済みか
    private bool isPlaced;

    // DOTweenで吸着中か
    private bool isSnapping;

    private Sequence snapSequence;
    private Sequence returnSequence;

    /// <summary>
    /// CountrySlot側から国IDを確認するために使用する。
    /// </summary>
    public string CountryId => countryId;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// ドラッグ開始。
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced || isSnapping)
        {
            return;
        }

        if (dragLayer == null)
        {
            Debug.LogError(
                $"{gameObject.name}: DragLayerが設定されていません。",
                gameObject
            );

            return;
        }

        wasPlacedSuccessfully = false;

        // =========================
        // 元の状態を保存
        // =========================

        originalParent = transform.parent;

        originalSiblingIndex =
            transform.GetSiblingIndex();

        originalAnchoredPosition =
            rectTransform.anchoredPosition;

        originalSizeDelta =
            rectTransform.sizeDelta;

        originalLocalScale =
            rectTransform.localScale;

        originalLocalRotation =
            rectTransform.localRotation;

        if (countryImageRect != null)
        {
            originalCountryImageSize =
                countryImageRect.sizeDelta;
        }

        if (cardBackgroundImage != null)
        {
            originalCardBackgroundEnabled =
                cardBackgroundImage.enabled;
        }

        if (countryNameText != null)
        {
            originalCountryNameActive =
                countryNameText.gameObject.activeSelf;
        }

        // ドロップ判定を邪魔しない
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = true;
        canvasGroup.ignoreParentGroups = true;

        // =========================
        // DragLayerへ移動
        // =========================

        rectTransform.SetParent(
            dragLayer,
            true
        );

        rectTransform.SetAsLastSibling();

        // =========================
        // カード表示を消す
        // =========================

        if (cardBackgroundImage != null)
        {
            cardBackgroundImage.enabled = false;
        }

        if (countryNameText != null)
        {
            countryNameText.gameObject.SetActive(false);
        }

        // =========================
        // 地図上の実寸サイズへ変更
        // =========================

        ResizeToMapSizeWhileDragging();

        // 指・マウス位置へ移動
        SetPositionFromPointer(eventData);
    }

    /// <summary>
    /// ドラッグ中。
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced || isSnapping)
        {
            return;
        }

        if (dragLayer == null)
        {
            return;
        }

        SetPositionFromPointer(eventData);
    }

    /// <summary>
    /// ドラッグ終了。
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // CountryDropTarget側ですでに正解済みなら終了
        if (wasPlacedSuccessfully)
        {
            return;
        }

        // =========================
        // 自分自身の正解Slotを直接確認
        // =========================

        GameObject correctSlotObject =
            GameObject.Find(countryId + "_Slot");

        if (correctSlotObject != null)
        {
            CountrySlot correctSlot =
                correctSlotObject.GetComponent<CountrySlot>();

            RectTransform dropTargetRect = null;

            Transform dropTarget =
                correctSlotObject.transform.Find("DropTarget");

            if (dropTarget != null)
            {
                dropTargetRect =
                    dropTarget as RectTransform;
            }

            if (
                correctSlot != null &&
                dropTargetRect != null
            )
            {
                bool inside =
                    RectTransformUtility.RectangleContainsScreenPoint(
                        dropTargetRect,
                        eventData.position,
                        eventData.pressEventCamera
                    );

                if (inside)
                {
                    // 自分の正解DropTarget内なら
                    // 他国のDropTargetが重なっていても正解にする
                    correctSlot.OnDrop(eventData);
                }
            }
        }

        // 正解処理が実行されたか再確認
        if (wasPlacedSuccessfully)
        {
            return;
        }

        // 本当に不正解なら元へ戻す
        ReturnToOriginalPositionWithTween();
    }


    /// <summary>
    /// 同じ国IDのSlotを探し、
    /// DragLayer上で見たときに
    /// Slotと同じ見た目サイズへ変更する。
    /// </summary>
    private void ResizeToMapSizeWhileDragging()
    {
        GameObject slotObject =
            GameObject.Find(
                countryId + "_Slot"
            );

        if (slotObject == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: " +
                $"{countryId}_Slot が見つかりません。"
            );

            return;
        }

        RectTransform slotRect =
            slotObject.GetComponent<RectTransform>();

        if (slotRect == null)
        {
            return;
        }

        /*
         * MapContentはズームされる可能性があるため、
         * slotRect.rect.sizeをそのまま使わず、
         * Slotのワールド上の大きさを
         * DragLayer座標へ変換する。
         */

        Vector3[] corners =
            new Vector3[4];

        slotRect.GetWorldCorners(corners);

        Vector3 bottomLeft =
            dragLayer.InverseTransformPoint(
                corners[0]
            );

        Vector3 topRight =
            dragLayer.InverseTransformPoint(
                corners[2]
            );

        Vector2 targetSize =
            new Vector2(
                Mathf.Abs(
                    topRight.x - bottomLeft.x
                ),
                Mathf.Abs(
                    topRight.y - bottomLeft.y
                )
            );

        rectTransform.sizeDelta =
            targetSize;

        if (countryImageRect != null)
        {
            countryImageRect.sizeDelta =
                targetSize;

            countryImageRect.anchoredPosition =
                Vector2.zero;
        }
    }

    /// <summary>
    /// マウス・指の位置へ移動する。
    /// </summary>
    private void SetPositionFromPointer(
        PointerEventData eventData
    )
    {
        Camera eventCamera =
            eventData.pressEventCamera;

        bool success =
            RectTransformUtility
                .ScreenPointToWorldPointInRectangle(
                    dragLayer,
                    eventData.position,
                    eventCamera,
                    out Vector3 worldPoint
                );

        if (!success)
        {
            return;
        }

        rectTransform.position =
            worldPoint;
    }

    /// <summary>
    /// 正解したピースをSlotへ吸着。
    /// CountrySlot側から呼ばれる。
    /// </summary>
    public void PlaceOnSlot(
        RectTransform slotRect,
        RectTransform placedCountryRoot
    )
    {
        if (slotRect == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Slotが設定されていません。",
                gameObject
            );

            return;
        }

        if (placedCountryRoot == null)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "PlacedCountryRootが設定されていません。",
                gameObject
            );

            return;
        }

        if (isPlaced || isSnapping)
        {
            return;
        }

        wasPlacedSuccessfully = true;
        isSnapping = true;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.ignoreParentGroups = false;

        snapSequence?.Kill();

        Vector3 slotWorldPosition =
            slotRect.position;

        // 正解時の本来サイズ
        Vector2 targetSize =
            slotRect.rect.size;

        rectTransform.SetParent(
            placedCountryRoot,
            true
        );

        rectTransform.SetAsLastSibling();

        // カード背景・国名は表示しない
        if (cardBackgroundImage != null)
        {
            cardBackgroundImage.enabled = false;
        }

        if (countryNameText != null)
        {
            countryNameText.gameObject.SetActive(false);
        }

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                placedRotationZ
            );

        snapSequence =
            DOTween.Sequence();

        // =========================
        // Slot位置へ移動
        // =========================

        snapSequence.Append(
            rectTransform
                .DOMove(
                    slotWorldPosition,
                    snapDuration
                )
                .SetEase(snapEase)
        );

        // =========================
        // Slotと同じサイズへ
        // =========================

        snapSequence.Join(
            rectTransform
                .DOSizeDelta(
                    targetSize,
                    snapDuration
                )
                .SetEase(snapEase)
        );

        if (countryImageRect != null)
        {
            snapSequence.Join(
                countryImageRect
                    .DOSizeDelta(
                        targetSize,
                        snapDuration
                    )
                    .SetEase(snapEase)
            );
        }

        // Scaleも正常化
        snapSequence.Join(
            rectTransform
                .DOScale(
                    placedLocalScale,
                    snapDuration
                )
                .SetEase(snapEase)
        );

        // 回転
        snapSequence.Join(
            rectTransform
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        0f,
                        placedRotationZ
                    ),
                    snapDuration,
                    RotateMode.Fast
                )
                .SetEase(snapEase)
        );

        // ポンッと演出
        if (
            punchScaleAmount > 0f &&
            punchDuration > 0f
        )
        {
            Vector3 punchAmount =
                placedLocalScale
                * punchScaleAmount;

            snapSequence.Append(
                rectTransform.DOPunchScale(
                    punchAmount,
                    punchDuration,
                    5,
                    0.5f
                )
            );
        }

        snapSequence.OnComplete(() =>
        {
            rectTransform.anchorMin =
                new Vector2(0.5f, 0.5f);

            rectTransform.anchorMax =
                new Vector2(0.5f, 0.5f);

            rectTransform.pivot =
                new Vector2(0.5f, 0.5f);

            Vector3 localSlotPosition =
                placedCountryRoot
                    .InverseTransformPoint(
                        slotRect.position
                    );

            rectTransform.localPosition =
                localSlotPosition;

            rectTransform.sizeDelta =
                targetSize;

            rectTransform.localScale =
                placedLocalScale;

            rectTransform.localRotation =
                targetRotation;

            if (countryImageRect != null)
            {
                countryImageRect.sizeDelta =
                    targetSize;

                countryImageRect.anchoredPosition =
                    Vector2.zero;
            }

            isPlaced = true;
            isSnapping = false;

            Debug.Log(
                $"{gameObject.name}をDOTweenで配置しました。"
            );
        });
    }

    /// <summary>
    /// カード表示を元へ戻す。
    /// </summary>
    private void RestoreCardVisual()
    {
        if (cardBackgroundImage != null)
        {
            cardBackgroundImage.enabled =
                originalCardBackgroundEnabled;
        }

        if (countryNameText != null)
        {
            countryNameText.gameObject.SetActive(
                originalCountryNameActive
            );
        }
    }

    /// <summary>
    /// 不正解時に揺れてから元の位置へ戻す。
    /// </summary>
    private void ReturnToOriginalPositionWithTween()
    {
        if (originalParent == null)
        {
            return;
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.ignoreParentGroups = false;

        returnSequence?.Kill();

        // カード背景と国名を復活
        RestoreCardVisual();

        Vector3 targetWorldPosition =
            originalParent.TransformPoint(
                new Vector3(
                    originalAnchoredPosition.x,
                    originalAnchoredPosition.y,
                    0f
                )
            );

        returnSequence =
            DOTween.Sequence();

        // 少し横へ揺らす
        if (
            wrongShakeStrength > 0f &&
            wrongShakeDuration > 0f
        )
        {
            returnSequence.Append(
                rectTransform.DOShakePosition(
                    wrongShakeDuration,
                    new Vector3(
                        wrongShakeStrength,
                        0f,
                        0f
                    ),
                    10,
                    60f,
                    false,
                    true
                )
            );
        }

        // 元の場所へ戻す
        returnSequence.Append(
            rectTransform
                .DOMove(
                    targetWorldPosition,
                    returnDuration
                )
                .SetEase(returnEase)
        );

        // Rootサイズを130x130等へ戻す
        returnSequence.Join(
            rectTransform
                .DOSizeDelta(
                    originalSizeDelta,
                    returnDuration
                )
                .SetEase(returnEase)
        );

        // Country_Imageも元サイズへ戻す
        if (countryImageRect != null)
        {
            returnSequence.Join(
                countryImageRect
                    .DOSizeDelta(
                        originalCountryImageSize,
                        returnDuration
                    )
                    .SetEase(returnEase)
            );
        }

        returnSequence.Join(
            rectTransform
                .DOScale(
                    originalLocalScale,
                    returnDuration
                )
                .SetEase(returnEase)
        );

        returnSequence.Join(
            rectTransform
                .DOLocalRotateQuaternion(
                    originalLocalRotation,
                    returnDuration
                )
                .SetEase(returnEase)
        );

        returnSequence.OnComplete(() =>
        {
            rectTransform.SetParent(
                originalParent,
                false
            );

            transform.SetSiblingIndex(
                originalSiblingIndex
            );

            rectTransform.anchoredPosition =
                originalAnchoredPosition;

            rectTransform.sizeDelta =
                originalSizeDelta;

            rectTransform.localScale =
                originalLocalScale;

            rectTransform.localRotation =
                originalLocalRotation;

            if (countryImageRect != null)
            {
                countryImageRect.sizeDelta =
                    originalCountryImageSize;
            }

            RestoreCardVisual();

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.ignoreParentGroups = false;
        });
    }

    private void OnDestroy()
    {
        snapSequence?.Kill();
        returnSequence?.Kill();
    }
}