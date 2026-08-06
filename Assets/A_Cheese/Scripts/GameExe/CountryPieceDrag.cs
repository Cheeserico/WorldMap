using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 国ピースのドラッグ、元の位置への復帰、
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

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // ドラッグ開始前の親
    private Transform originalParent;

    // ドラッグ開始前のHierarchy上の並び順
    private int originalSiblingIndex;

    // ドラッグ開始前の位置
    private Vector2 originalAnchoredPosition;

    // ドラッグ開始前の大きさ
    private Vector3 originalLocalScale;

    // ドラッグ開始前の角度
    private Quaternion originalLocalRotation;

    // 今回のドラッグで正解したか
    private bool wasPlacedSuccessfully;

    // すでに地図へ配置済みか
    private bool isPlaced;

    // DOTweenで吸着中か
    private bool isSnapping;

    // 実行中のDOTween
    private Sequence snapSequence;

    [Header("不正解時のDOTween演出")]
    [SerializeField]
    private float returnDuration = 0.25f;

    [SerializeField]
    private Ease returnEase = Ease.OutCubic;

    [SerializeField]
    private float wrongShakeStrength = 12f;

    [SerializeField]
    private float wrongShakeDuration = 0.18f;



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
    /// ドラッグ開始時。
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 配置済み、または吸着演出中ならドラッグさせない
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

        // 今回のドラッグ結果を初期化
        wasPlacedSuccessfully = false;

        // 元の状態を記録
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalLocalScale = rectTransform.localScale;
        originalLocalRotation = rectTransform.localRotation;

        // ドラッグ中はピース自身がドロップ判定を邪魔しないようにする
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = true;
        canvasGroup.ignoreParentGroups = true;

        // DragLayerの子へ移動
        rectTransform.SetParent(dragLayer, true);
        rectTransform.SetAsLastSibling();

        // マウス・指の位置へ移動
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
    /// マウスボタン・指を離したとき。
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // 正解ならDOTween吸着処理に任せる
        if (wasPlacedSuccessfully)
        {
            return;
        }

        // 不正解、またはスロット外ならDOTweenで戻す
        ReturnToOriginalPositionWithTween();
    }
    /// <summary>
    /// マウス・指の位置へピースを移動する。
    /// </summary>
    private void SetPositionFromPointer(
        PointerEventData eventData
    )
    {
        Camera eventCamera = eventData.pressEventCamera;

        bool success =
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragLayer,
                eventData.position,
                eventCamera,
                out Vector3 worldPoint
            );

        if (!success)
        {
            return;
        }

        rectTransform.position = worldPoint;
    }

    /// <summary>
    /// 正解したピースをDOTweenでスロットへ吸着させる。
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
                $"{gameObject.name}: PlacedCountryRootが設定されていません。",
                gameObject
            );

            return;
        }

        if (isPlaced || isSnapping)
        {
            return;
        }

        /*
         * OnEndDragで元へ戻らないように、
         * DOTween開始前に成功状態へする。
         */
        wasPlacedSuccessfully = true;
        isSnapping = true;

        // 吸着中・配置後は操作不可
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.ignoreParentGroups = false;

        // 同じピースに古いTweenが残っていたら停止
        snapSequence?.Kill();

        /*
         * スロットの位置をワールド座標で記録する。
         * 以前、位置が正しく合った方法を使用する。
         */
        Vector3 slotWorldPosition = slotRect.position;

        /*
         * PlacedCountryRootへ移動。
         * trueにすることで、親変更直後の見た目を維持する。
         */
        rectTransform.SetParent(placedCountryRoot, true);
        rectTransform.SetAsLastSibling();

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                placedRotationZ
            );

        snapSequence = DOTween.Sequence();

        /*
         * スロット位置への移動。
         */
        snapSequence.Append(
            rectTransform
                .DOMove(
                    slotWorldPosition,
                    snapDuration
                )
                .SetEase(snapEase)
        );

        /*
         * 移動と同時に配置後サイズへ変える。
         */
        snapSequence.Join(
            rectTransform
                .DOScale(
                    placedLocalScale,
                    snapDuration
                )
                .SetEase(snapEase)
        );

        /*
         * 移動と同時に配置後角度へ回転する。
         */
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

        /*
         * 配置後に少しだけポンッと弾ませる。
         */
        if (
            punchScaleAmount > 0f &&
            punchDuration > 0f
        )
        {
            Vector3 punchAmount =
                placedLocalScale * punchScaleAmount;

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
            /*
             * AnchorとPivotを中央へ統一する。
             */
            rectTransform.anchorMin =
                new Vector2(0.5f, 0.5f);

            rectTransform.anchorMax =
                new Vector2(0.5f, 0.5f);

            rectTransform.pivot =
                new Vector2(0.5f, 0.5f);

            /*
             * PlacedCountryRoot内の座標へ変換する。
             * 以前、右へずれる問題を直せた方式。
             */
            Vector3 localSlotPosition =
                placedCountryRoot.InverseTransformPoint(
                    slotRect.position
                );

            rectTransform.localPosition =
                localSlotPosition;

            // DOTween後の細かな誤差を修正
            rectTransform.localScale =
                placedLocalScale;

            rectTransform.localRotation =
                targetRotation;

            // 正式に配置済みにする
            isPlaced = true;
            isSnapping = false;

            Debug.Log(
                $"{gameObject.name}をDOTweenで配置しました。"
            );
        });
    }

    /// <summary>
    /// 不正解時に元のPieceContentへ戻す。
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        if (originalParent == null)
        {
            return;
        }

        rectTransform.SetParent(
            originalParent,
            false
        );

        transform.SetSiblingIndex(
            originalSiblingIndex
        );

        rectTransform.anchoredPosition =
            originalAnchoredPosition;

        rectTransform.localScale =
            originalLocalScale;

        rectTransform.localRotation =
            originalLocalRotation;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.ignoreParentGroups = false;
    }

    /// <summary>
    /// 不正解時に少し揺れてから元の位置へ戻す。
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

        /*
         * 元の親オブジェクト内での位置を
         * ワールド座標として取得する。
         */
        Vector3 targetWorldPosition =
            originalParent.TransformPoint(
                new Vector3(
                    originalAnchoredPosition.x,
                    originalAnchoredPosition.y,
                    0f
                )
            );

        returnSequence = DOTween.Sequence();

        /*
         * 最初に少し横へ揺らす。
         */
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

        /*
         * 元の場所へ移動。
         */
        returnSequence.Append(
            rectTransform
                .DOMove(
                    targetWorldPosition,
                    returnDuration
                )
                .SetEase(returnEase)
        );

        /*
         * 元のサイズと角度へ戻す。
         */
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
            /*
             * アニメーション終了後にPieceContentへ戻す。
             */
            rectTransform.SetParent(
                originalParent,
                false
            );

            transform.SetSiblingIndex(
                originalSiblingIndex
            );

            rectTransform.anchoredPosition =
                originalAnchoredPosition;

            rectTransform.localScale =
                originalLocalScale;

            rectTransform.localRotation =
                originalLocalRotation;

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.ignoreParentGroups = false;
        });
    }

    /// <summary>
    /// オブジェクト削除時にTweenを停止する。
    /// </summary>
    private void OnDestroy()
    {
        snapSequence?.Kill();
        returnSequence?.Kill();
    }
}