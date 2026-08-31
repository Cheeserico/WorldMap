using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

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
    IEndDragHandler,
    IPointerDownHandler,
    IPointerUpHandler

{
    [Header("このピースの国ID")]
    [SerializeField]
    private string countryId;


    [Header("地図操作")]
    [SerializeField]
    private MapPanZoomController mapPanZoomController;

    /// <summary>
    /// CountrySlot側から国IDを確認するために使用する。
    /// </summary>

    public string CountryId => countryId;

    public bool IsPlaced => isPlaced;
    public bool IsSnapping => isSnapping;

    // ドラッグ元の場所を維持する透明な仮置き
    private RectTransform dragPlaceholder;

    public string CountryName
    {
        get
        {
            if (countryNameText != null)
            {
                return countryNameText.text;
            }

            return countryId;
        }
    }

    [Header("ドラッグ中のピースを置く場所")]
    [SerializeField]
    private RectTransform dragLayer;

    [Header("ピース表示")]
    [SerializeField]
    private RectTransform countryImageRect;

    [SerializeField]
    private Image countryFlagImage;

    [SerializeField]
    private Image cardBackgroundImage;

    [SerializeField]
    private TMP_Text countryNameText;

    [Header("正解・不正解表示")]
    [SerializeField]
    private AnswerFeedbackUI answerFeedbackUI;

    [Header("国旗データ")]
    [SerializeField]
    private CountryFlagDatabase countryFlagDatabase;


    [Header("カード表示用シルエット設定")]

    [SerializeField]
    private Image countryImage;

    [Tooltip("カード内で国画像を表示する最大サイズ")]
    [SerializeField]

    private float cardCountryMaxSize = 115f;

    [Tooltip("このサイズ未満の国を低解像度の小国として扱う")]
    [SerializeField]
    private int smallCountryPixelThreshold = 32;

    // 地図用の元Sprite
    private Sprite originalCountrySprite;

    // カード表示専用の、透明余白を除いたSprite
    private Sprite cardCountrySprite;


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

    // ヒント選択のタップ中はドラッグさせない
    private bool isHintPointerInteraction;

    private Sequence snapSequence;
    private Sequence returnSequence;

    private Tween hintHighlightTween;

    // 国旗の元状態
    private bool originalCountryFlagActive;

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvasGroup =
            GetComponent<CanvasGroup>();

        if (answerFeedbackUI == null)
        {
            answerFeedbackUI =
                FindFirstObjectByType<AnswerFeedbackUI>();
        }

        if (countryFlagDatabase == null)
        {
            countryFlagDatabase =
                FindFirstObjectByType<CountryFlagDatabase>();
        }

        if (mapPanZoomController == null)
        {
            mapPanZoomController =
                FindFirstObjectByType<MapPanZoomController>();
        }
    }

    private void Start()
    {
        SetupCountryFlag();

        CreateCardCountrySprite();
        ApplyCardCountryVisual();
    }
    /// <summary>
    /// ヒント選択中にカードを押したときの処理。
    /// </summary>
    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (isPlaced || isSnapping)
        {
            return;
        }

        HintManager hintManager =
            FindFirstObjectByType<HintManager>();

        if (hintManager == null ||
            !hintManager.IsSelectingCountry)
        {
            return;
        }

        // この指では通常ドラッグを開始させない
        isHintPointerInteraction = true;

        hintManager.SelectCountryForHint(
            this
        );
    }

    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        isHintPointerInteraction = false;
    }

    /// <summary>
    /// ドラッグ開始。
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {

        if (isHintPointerInteraction)
        {
            return;
        }

        if (isPlaced || isSnapping)
        {
            return;
        }

        // StopHintHighlight();

        if (dragLayer == null)
        {
            Debug.LogError(
                $"{gameObject.name}: DragLayerが設定されていません。",
                gameObject
            );

            return;
        }

        if (mapPanZoomController != null)
        {
            mapPanZoomController.SetPieceInteractionLocked(true);
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



        if (countryFlagImage != null)
        {
            originalCountryFlagActive =
                countryFlagImage.gameObject.activeSelf;
        }

        // ドロップ判定を邪魔しない
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = true;
        canvasGroup.ignoreParentGroups = true;

        // ドラッグ元のカード位置を維持する
        CreateDragPlaceholder();


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


        if (countryFlagImage != null)
        {
            countryFlagImage.gameObject.SetActive(false);
        }

        if (countryImage != null && originalCountrySprite != null)
        {
            countryImage.sprite = originalCountrySprite;
        }

        // =========================
        // 地図上の実寸サイズへ変更
        // =========================

        ResizeToMapSizeWhileDragging();

        // 指・マウス位置へ移動
        SetPositionFromPointer(eventData);
    }

    private void ApplyCardCountryVisual()
    {
        if (countryImage == null)
        {
            return;
        }

        if (cardCountrySprite == null)
        {
            return;
        }

        // カード表示用Spriteへ切り替え
        countryImage.sprite = cardCountrySprite;

        // 縦横比を維持
        countryImage.preserveAspect = true;

        float spriteWidth = cardCountrySprite.rect.width;
        float spriteHeight = cardCountrySprite.rect.height;

        if (spriteWidth <= 0f || spriteHeight <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(
            cardCountryMaxSize / spriteWidth,
            cardCountryMaxSize / spriteHeight
        );

        float targetWidth = spriteWidth * scale;
        float targetHeight = spriteHeight * scale;

        countryImageRect.sizeDelta = new Vector2(
            targetWidth,
            targetHeight
        );

        countryImageRect.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// ドラッグ中。
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (isHintPointerInteraction)
        {
            return;
        }

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
        if (isHintPointerInteraction)
        {
            return;
        }

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

        // ==================================================
        // ここまで正解にならなかった = 不正解
        // ==================================================

        Debug.Log(
            $"{gameObject.name} は不正解です。ID：{countryId}"
        );

        // 不正解SE
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(SEType.Wrong);
        }

        // 不正解表示
        if (answerFeedbackUI != null &&
            countryFlagDatabase != null)
        {
            Debug.Log("★ AnswerFeedbackUIを呼びます");

            Sprite flag =
                countryFlagDatabase.GetFlag(
                    countryId
                );

            answerFeedbackUI.ShowWrong(
                countryId,
                flag
            );
        }
        else
        {
            Debug.LogWarning(
                $"★ Feedback参照なし " +
                $"AnswerFeedbackUI={answerFeedbackUI} " +
                $"CountryFlagDatabase={countryFlagDatabase}"
            );
        }

        // 元へ戻す
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

        StopHintHighlight();


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

        if (countryFlagImage != null)
        {
            countryFlagImage.gameObject.SetActive(false);
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
                rectTransform.DOPunchScale(punchAmount,
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

            // 正解したのでカード一覧の空きを詰める
            RemoveDragPlaceholder();

            isPlaced = true;
            isSnapping = false;

            if (mapPanZoomController != null)
            {
                mapPanZoomController.SetPieceInteractionLocked(false);
            }

            Debug.Log(
                $"{gameObject.name}をDOTweenで配置しました。"
            );
        });
    }

    /// <summary>
    /// 途中保存されていた配置済みピースを、
    /// DOTween演出なしで正しいスロット位置へ復元する。
    /// </summary>
    public void RestoreOnSlot(
        RectTransform slotRect,
        RectTransform placedCountryRoot
    )
    {
        if (slotRect == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}：" +
                "復元先のSlotがありません。"
            );

            return;
        }

        if (placedCountryRoot == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}：" +
                "PlacedCountryRootがありません。"
            );

            return;
        }

        // 実行中の演出を停止
        snapSequence?.Kill();
        returnSequence?.Kill();

        StopHintHighlight();

        wasPlacedSuccessfully = true;
        isSnapping = false;
        isPlaced = true;
        isHintPointerInteraction = false;

        // 配置済みピースの親へ移動
        rectTransform.SetParent(
            placedCountryRoot,
            false
        );

        rectTransform.SetAsLastSibling();

        rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        // Slotのワールド位置を
        // PlacedCountryRoot内の位置へ変換
        Vector3 localSlotPosition =
            placedCountryRoot.InverseTransformPoint(
                slotRect.position
            );

        rectTransform.localPosition =
            localSlotPosition;

        Vector2 targetSize =
            slotRect.rect.size;

        rectTransform.sizeDelta =
            targetSize;

        rectTransform.localScale =
            placedLocalScale;

        rectTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                placedRotationZ
            );

        // カード背景と国名は表示しない
        if (cardBackgroundImage != null)
        {
            cardBackgroundImage.enabled = false;
        }

        if (countryNameText != null)
        {
            countryNameText.gameObject.SetActive(
                false
            );
        }

        if (countryFlagImage != null)
        {
            countryFlagImage.gameObject.SetActive(false);
        }

        // カード専用画像ではなく地図用画像へ戻す
        if (countryImage != null &&
            originalCountrySprite != null)
        {
            countryImage.sprite =
                originalCountrySprite;
        }

        if (countryImageRect != null)
        {
            countryImageRect.sizeDelta =
                targetSize;

            countryImageRect.anchoredPosition =
                Vector2.zero;
        }

        // 配置済みなので操作不可
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.ignoreParentGroups = false;
        }

        Debug.Log(
            $"{gameObject.name}を途中データから復元しました。"
        );
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

        if (countryFlagImage != null)
        {
            countryFlagImage.gameObject.SetActive(
                originalCountryFlagActive
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
        //RestoreCardVisual();

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

            // 不正解なので仮置きとピースを入れ替える
            RemoveDragPlaceholder();

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

            // カード用の見やすいシルエットへ戻す
            ApplyCardCountryVisual();

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.ignoreParentGroups = false;

            if (mapPanZoomController != null)
            {
                mapPanZoomController.SetPieceInteractionLocked(false);
            }

        });
    }

    // 透明余白を無視してカード用Spriteを自動生成する処理
    private void CreateCardCountrySprite()
    {
        if (countryImage == null)
        {
            return;
        }

        if (countryImage.sprite == null)
        {
            return;
        }

        originalCountrySprite = countryImage.sprite;

        Texture2D sourceTexture = originalCountrySprite.texture;

        if (sourceTexture == null)
        {
            return;
        }

        Rect spriteRect = originalCountrySprite.textureRect;

        int width = Mathf.RoundToInt(spriteRect.width);
        int height = Mathf.RoundToInt(spriteRect.height);

        Color[] pixels = sourceTexture.GetPixels(
            Mathf.RoundToInt(spriteRect.x),
            Mathf.RoundToInt(spriteRect.y),
            width,
            height
        );

        bool[] visited = new bool[width * height];

        List<List<Vector2Int>> groups =
            new List<List<Vector2Int>>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                if (visited[index])
                {
                    continue;
                }

                Color pixel = pixels[index];

                if (pixel.a <= 0.5f)
                {
                    visited[index] = true;
                    continue;
                }

                List<Vector2Int> group =
                    FindConnectedPixels(
                        x,
                        y,
                        width,
                        height,
                        pixels,
                        visited
                    );

                groups.Add(group);
            }
        }

        if (groups.Count == 0)
        {
            return;
        }

        // 一番大きい陸地を探す
        int largestGroupSize = 0;

        foreach (List<Vector2Int> group in groups)
        {
            if (group.Count > largestGroupSize)
            {
                largestGroupSize = group.Count;
            }
        }

        // 最大陸地の何％以上ならカード表示範囲に含めるか
        float includeRatio = 0.2f;

        int minX = width;
        int minY = height;
        int maxX = 0;
        int maxY = 0;

        bool foundIncludedGroup = false;

        foreach (List<Vector2Int> group in groups)
        {
            float ratio =
                (float)group.Count / largestGroupSize;

            // 小さすぎる離島はサイズ計算から除外
            if (ratio < includeRatio)
            {
                continue;
            }

            foundIncludedGroup = true;

            foreach (Vector2Int pixel in group)
            {
                if (pixel.x < minX) minX = pixel.x;
                if (pixel.x > maxX) maxX = pixel.x;

                if (pixel.y < minY) minY = pixel.y;
                if (pixel.y > maxY) maxY = pixel.y;
            }
        }

        if (!foundIncludedGroup)
        {
            return;
        }

        int croppedWidth = maxX - minX + 1;
        int croppedHeight = maxY - minY + 1;

        bool isLowResolutionCountry =
            Mathf.Max(croppedWidth, croppedHeight)
            < smallCountryPixelThreshold;

        if (isLowResolutionCountry)
        {
            /*
            Debug.LogWarning(
                $"【低解像度の小国】{countryId}: " +
                $"切り抜き後={croppedWidth}x{croppedHeight}px",
                gameObject
            );
            */

            // Resources/CardCountryPiecesから
            // カード専用の高解像度Spriteを読み込む
            Sprite highResolutionCardSprite =
                Resources.Load<Sprite>(
                    $"CardCountryPieces/{countryId}"
                );

            if (highResolutionCardSprite != null)
            {
                cardCountrySprite =
                    highResolutionCardSprite;

                /*
                Debug.Log(
                    $"【高解像度カード使用】{countryId}: " +
                    $"{highResolutionCardSprite.rect.width}x" +
                    $"{highResolutionCardSprite.rect.height}px",
                    gameObject
                );
                */
                return;
            }
            /*
            Debug.LogWarning(
                $"【カード画像なし】" +
                $"Resources/CardCountryPieces/{countryId} " +
                $"が見つからないため、元Spriteを使用します。",
                gameObject
            
            );
            */
        }

/*
        Debug.Log(
    $"【カード画像確認】国ID={countryId}, " +
    $"元Sprite={width}x{height}px, " +
    $"切り抜き後={croppedWidth}x{croppedHeight}px, " +
    $"カード表示最大辺={cardCountryMaxSize}px, " +
    $"Texture={sourceTexture.width}x{sourceTexture.height}px, " +
    $"FilterMode={sourceTexture.filterMode}",
    gameObject
);
*/

        Rect croppedRect = new Rect(
            spriteRect.x + minX,
            spriteRect.y + minY,
            croppedWidth,
            croppedHeight
        );

        cardCountrySprite = Sprite.Create(
            sourceTexture,
            croppedRect,
            new Vector2(0.5f, 0.5f),
            originalCountrySprite.pixelsPerUnit
        );
    }

    private List<Vector2Int> FindConnectedPixels(
    int startX,
    int startY,
    int width,
    int height,
    Color[] pixels,
    bool[] visited
)
    {
        List<Vector2Int> result =
            new List<Vector2Int>();

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        queue.Enqueue(
            new Vector2Int(startX, startY)
        );

        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();

            int x = current.x;
            int y = current.y;

            if (
                x < 0 ||
                x >= width ||
                y < 0 ||
                y >= height
            )
            {
                continue;
            }

            int index = y * width + x;

            if (visited[index])
            {
                continue;
            }

            visited[index] = true;

            if (pixels[index].a <= 0.5f)
            {
                continue;
            }

            result.Add(current);

            queue.Enqueue(
                new Vector2Int(x + 1, y)
            );

            queue.Enqueue(
                new Vector2Int(x - 1, y)
            );

            queue.Enqueue(
                new Vector2Int(x, y + 1)
            );

            queue.Enqueue(
                new Vector2Int(x, y - 1)
            );
        }

        return result;
    }

    /// <summary>
    /// ヒント対象カードの点滅を開始する。
    /// </summary>
    public void PlayHintHighlight()
    {
        if (isPlaced || isSnapping)
        {
            return;
        }

        StopHintHighlight();

        canvasGroup.alpha = 1f;

        hintHighlightTween =
            canvasGroup
                .DOFade(
                    0.4f,
                    0.35f
                )
                .SetLoops(
                    -1,
                    LoopType.Yoyo
                )
                .SetEase(
                    Ease.InOutSine
                );
    }

    /// <summary>
    /// ヒント対象カードの点滅を終了する。
    /// </summary>
    public void StopHintHighlight()
    {
        hintHighlightTween?.Kill();
        hintHighlightTween = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }


    private void OnDestroy()
    {
        snapSequence?.Kill();
        returnSequence?.Kill();
        hintHighlightTween?.Kill();
    }

    /// <summary>
    /// ドラッグ中も元のカード位置を維持する。
    /// </summary>
    private void CreateDragPlaceholder()
    {
        RemoveDragPlaceholder();

        if (originalParent == null)
        {
            return;
        }

        GameObject placeholderObject =
            new GameObject(
                $"{gameObject.name}_Placeholder",
                typeof(RectTransform),
                typeof(LayoutElement)
            );

        dragPlaceholder =
            placeholderObject.GetComponent<RectTransform>();

        dragPlaceholder.SetParent(
            originalParent,
            false
        );

        dragPlaceholder.SetSiblingIndex(
            originalSiblingIndex
        );

        dragPlaceholder.sizeDelta =
            originalSizeDelta;

        LayoutElement placeholderLayout =
            placeholderObject.GetComponent<LayoutElement>();

        LayoutElement pieceLayout =
            GetComponent<LayoutElement>();

        if (pieceLayout != null)
        {
            placeholderLayout.minWidth =
                pieceLayout.minWidth;

            placeholderLayout.minHeight =
                pieceLayout.minHeight;

            placeholderLayout.preferredWidth =
                pieceLayout.preferredWidth;

            placeholderLayout.preferredHeight =
                pieceLayout.preferredHeight;

            placeholderLayout.flexibleWidth =
                pieceLayout.flexibleWidth;

            placeholderLayout.flexibleHeight =
                pieceLayout.flexibleHeight;
        }
        else
        {
            placeholderLayout.preferredWidth =
                originalSizeDelta.x;

            placeholderLayout.preferredHeight =
                originalSizeDelta.y;

            placeholderLayout.flexibleWidth = 0f;
            placeholderLayout.flexibleHeight = 0f;
        }
    }

    /// <summary>
    /// ドラッグ元に残した仮置きを削除する。
    /// </summary>
    private void RemoveDragPlaceholder()
    {
        if (dragPlaceholder == null)
        {
            return;
        }

        Destroy(dragPlaceholder.gameObject);
        dragPlaceholder = null;
    }

    private void OnDisable()
    {
        snapSequence?.Kill();
        returnSequence?.Kill();

        RemoveDragPlaceholder();

        if (mapPanZoomController != null)
        {
            mapPanZoomController
                .SetPieceInteractionLocked(false);
        }
    }

    /// <summary>
    /// 国IDに対応する国旗をカードへ設定する。
    /// </summary>
    private void SetupCountryFlag()
    {
        if (countryFlagImage == null)
        {
            return;
        }

        if (countryFlagDatabase == null)
        {
            countryFlagDatabase =
                FindFirstObjectByType<CountryFlagDatabase>();
        }

        if (countryFlagDatabase == null)
        {
            countryFlagImage.gameObject.SetActive(false);
            return;
        }

        Sprite flagSprite =
            countryFlagDatabase.GetFlag(
                countryId
            );

        if (flagSprite == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}：" +
                $"国旗が見つかりません。CountryId={countryId}",
                gameObject
            );

            countryFlagImage.gameObject.SetActive(false);
            return;
        }

        countryFlagImage.sprite =
            flagSprite;

        countryFlagImage.preserveAspect =
            true;

        countryFlagImage.gameObject.SetActive(
            true
        );
    }
}