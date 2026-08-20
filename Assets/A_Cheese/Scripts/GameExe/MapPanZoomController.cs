using DG.Tweening;
using UnityEngine;


/// <summary>
/// 世界地図の移動と拡大縮小を管理します。
///
/// 対応操作：
/// ・マウス左ドラッグで移動
/// ・マウスホイールで拡大縮小
/// ・スマートフォンの1本指で移動
/// ・スマートフォンの2本指ピンチで拡大縮小
/// </summary>
public class MapPanZoomController : MonoBehaviour
{
    [Header("操作対象")]
    [SerializeField] private RectTransform mapViewport;
    [SerializeField] private RectTransform mapContent;

    [Header("拡大縮小")]
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 3f;

    [Tooltip("マウスホイールの拡大縮小速度")]
    [SerializeField] private float mouseWheelZoomSpeed = 0.2f;

    [Tooltip("スマートフォンのピンチ拡大縮小速度")]
    [SerializeField] private float pinchZoomSpeed = 1f;

    [Header("移動")]
    [Tooltip("地図移動の感度")]
    [SerializeField] private float moveSensitivity = 1f;

    private Canvas parentCanvas;
    private Camera uiCamera;

    // マウス操作用
    private bool isMouseDragging;
    private Vector2 previousMouseLocalPosition;

    // タッチ操作用
    private bool isTouchDragging;
    private Vector2 previousTouchLocalPosition;

    [Header("ヒントによる自動移動")]
    [SerializeField]
    private float hintFocusDuration = 0.6f;

    [SerializeField]
    private Ease hintFocusEase = Ease.InOutCubic;

    // 自動移動中は手動操作を停止する
    private bool isInputLocked;

    private Sequence hintFocusSequence;

    // ==================================================
    // 途中保存用
    // ==================================================

    /// <summary>
    /// 現在のMapContent位置。
    /// </summary>
    public Vector2 CurrentMapPosition
    {
        get
        {
            if (mapContent == null)
            {
                return Vector2.zero;
            }

            return mapContent.anchoredPosition;
        }
    }

    /// <summary>
    /// 現在のMapContent拡大率。
    /// </summary>
    public float CurrentMapScale
    {
        get
        {
            if (mapContent == null)
            {
                return 1f;
            }

            return mapContent.localScale.x;
        }
    }


    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            Debug.LogError("親にCanvasが見つかりません。");
            enabled = false;
            return;
        }

        /*
         * Screen Space - Overlayならカメラはnull。
         * Screen Space - Cameraなどの場合はCanvasのカメラを使用します。
         */
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = null;
        }
        else
        {
            uiCamera = parentCanvas.worldCamera;
        }

        if (mapViewport == null || mapContent == null)
        {
            Debug.LogError(
                "MapPanZoomControllerのMapViewportまたはMapContentが設定されていません。"
            );

            enabled = false;
            return;
        }
    }

    private void Start()
    {
        ApplyStageStartView();
    }

    /// <summary>
    /// StageDataに設定された開始位置・開始倍率を地図へ反映します。
    /// </summary>
    private void ApplyStageStartView()
    {
        StageData stageData = null;

        // StageSelectSceneから来た場合
        if (StageManager.Instance != null)
        {
            stageData =
                StageManager.Instance.SelectedStageData;
        }

        // StageDataが取得できなかった場合は、
        // 今まで通りの初期状態を使用
        if (stageData == null)
        {
            SetScale(
                Mathf.Clamp(
                    mapContent.localScale.x,
                    minScale,
                    maxScale
                )
            );

            ClampMapPosition();

            Debug.Log(
                "MapPanZoomController：StageDataがないため通常の初期位置を使用します。"
            );

            return;
        }

        // ------------------------------
        // 開始倍率
        // ------------------------------

        float startScale =
            Mathf.Clamp(
                stageData.mapStartScale,
                minScale,
                maxScale
            );

        SetScale(startScale);


        // ------------------------------
        // 開始位置
        // ------------------------------

        mapContent.anchoredPosition =
            stageData.mapStartPosition;


        // 移動可能範囲の中へ収める
        ClampMapPosition();


        Debug.Log(
            $"MapPanZoomController：" +
            $"{stageData.stageName} の開始表示を設定しました。 " +
            $"Position={mapContent.anchoredPosition}, " +
            $"Scale={startScale}"
        );
    }

    private void Update()
    {
        if (isInputLocked)
        {
            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#endif

#if UNITY_ANDROID || UNITY_IOS
        HandleTouchInput();
#endif
    }

    /// <summary>
    /// マウス操作を処理します。
    /// </summary>
    private void HandleMouseInput()
    {
        Vector2 mouseScreenPosition = Input.mousePosition;

        // マウスカーソルがMapViewport内にあるか確認します。
        bool isPointerInsideViewport =
            RectTransformUtility.RectangleContainsScreenPoint(
                mapViewport,
                mouseScreenPosition,
                uiCamera
            );

        /*
         * 左クリックを開始した瞬間。
         * MapViewport内からドラッグを始めた場合だけ地図を動かします。
         */
        if (Input.GetMouseButtonDown(0) && isPointerInsideViewport)
        {
            if (TryGetViewportLocalPosition(
                    mouseScreenPosition,
                    out previousMouseLocalPosition))
            {
                isMouseDragging = true;
            }
        }

        // 左クリック中のドラッグ移動
        if (Input.GetMouseButton(0) && isMouseDragging)
        {
            if (TryGetViewportLocalPosition(
                    mouseScreenPosition,
                    out Vector2 currentMouseLocalPosition))
            {
                Vector2 delta =
                    currentMouseLocalPosition - previousMouseLocalPosition;

                MoveMap(delta * moveSensitivity);

                previousMouseLocalPosition = currentMouseLocalPosition;
            }
        }

        // 左クリックを離した
        if (Input.GetMouseButtonUp(0))
        {
            isMouseDragging = false;
        }

        // MapViewport内にマウスがある場合だけホイール操作を受け付けます。
        if (isPointerInsideViewport)
        {
            float wheel = Input.mouseScrollDelta.y;

            if (Mathf.Abs(wheel) > 0.01f)
            {
                float scaleMultiplier =
                    1f + wheel * mouseWheelZoomSpeed;

                ZoomAtScreenPosition(
                    mouseScreenPosition,
                    scaleMultiplier
                );
            }
        }
    }

    /// <summary>
    /// スマートフォンのタッチ操作を処理します。
    /// </summary>
    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            isTouchDragging = false;
            return;
        }

        // 2本指以上ならピンチ操作
        if (Input.touchCount >= 2)
        {
            isTouchDragging = false;

            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 currentPosition1 = touch1.position;
            Vector2 currentPosition2 = touch2.position;

            Vector2 previousPosition1 =
                currentPosition1 - touch1.deltaPosition;

            Vector2 previousPosition2 =
                currentPosition2 - touch2.deltaPosition;

            float currentDistance =
                Vector2.Distance(currentPosition1, currentPosition2);

            float previousDistance =
                Vector2.Distance(previousPosition1, previousPosition2);

            if (previousDistance <= 0.01f)
            {
                return;
            }

            float pinchRatio = currentDistance / previousDistance;

            /*
             * pinchZoomSpeedが1なら、そのままピンチ量を使用します。
             * 小さくすると拡大縮小がゆっくりになります。
             */
            float adjustedRatio =
                Mathf.Lerp(1f, pinchRatio, pinchZoomSpeed);

            Vector2 pinchCenter =
                (currentPosition1 + currentPosition2) * 0.5f;

            // ピンチの中心がMapViewport内の場合だけ操作します。
            bool isPinchInsideViewport =
                RectTransformUtility.RectangleContainsScreenPoint(
                    mapViewport,
                    pinchCenter,
                    uiCamera
                );

            if (isPinchInsideViewport)
            {
                ZoomAtScreenPosition(
                    pinchCenter,
                    adjustedRatio
                );
            }

            return;
        }

        // ここから1本指操作
        Touch touch = Input.GetTouch(0);

        bool isTouchInsideViewport =
            RectTransformUtility.RectangleContainsScreenPoint(
                mapViewport,
                touch.position,
                uiCamera
            );

        if (touch.phase == TouchPhase.Began)
        {
            if (!isTouchInsideViewport)
            {
                isTouchDragging = false;
                return;
            }

            if (TryGetViewportLocalPosition(
                    touch.position,
                    out previousTouchLocalPosition))
            {
                isTouchDragging = true;
            }
        }

        if (touch.phase == TouchPhase.Moved && isTouchDragging)
        {
            if (TryGetViewportLocalPosition(
                    touch.position,
                    out Vector2 currentTouchLocalPosition))
            {
                Vector2 delta =
                    currentTouchLocalPosition - previousTouchLocalPosition;

                MoveMap(delta * moveSensitivity);

                previousTouchLocalPosition = currentTouchLocalPosition;
            }
        }

        if (touch.phase == TouchPhase.Ended ||
            touch.phase == TouchPhase.Canceled)
        {
            isTouchDragging = false;
        }
    }

    /// <summary>
    /// 地図を移動します。
    /// </summary>
    private void MoveMap(Vector2 delta)
    {
        mapContent.anchoredPosition += delta;
        ClampMapPosition();
    }

    /// <summary>
    /// 指またはマウスカーソルの位置を中心に拡大縮小します。
    /// </summary>
    private void ZoomAtScreenPosition(
        Vector2 screenPosition,
        float scaleMultiplier)
    {
        if (!TryGetViewportLocalPosition(
                screenPosition,
                out Vector2 focusLocalPosition))
        {
            return;
        }

        float oldScale = mapContent.localScale.x;

        float newScale = Mathf.Clamp(
            oldScale * scaleMultiplier,
            minScale,
            maxScale
        );

        if (Mathf.Approximately(oldScale, newScale))
        {
            return;
        }

        /*
         * 拡大縮小前に、カーソル・指が地図上のどの位置を
         * 指していたかを計算します。
         */
        Vector2 contentPoint =
            (focusLocalPosition - mapContent.anchoredPosition) / oldScale;

        SetScale(newScale);

        /*
         * 拡大縮小後も、同じ地図上の場所が
         * 指・カーソルの下に残るよう位置を調整します。
         */
        mapContent.anchoredPosition =
            focusLocalPosition - contentPoint * newScale;

        ClampMapPosition();
    }

    /// <summary>
    /// MapContentの拡大率を設定します。
    /// </summary>
    private void SetScale(float scale)
    {
        mapContent.localScale =
            new Vector3(scale, scale, 1f);
    }

    /// <summary>
    /// 画面座標をMapViewport内のローカル座標へ変換します。
    /// </summary>
    private bool TryGetViewportLocalPosition(
        Vector2 screenPosition,
        out Vector2 localPosition)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapViewport,
            screenPosition,
            uiCamera,
            out localPosition
        );
    }

    /// <summary>
    /// 地図がMapViewportの外へ行きすぎないよう制限します。
    /// </summary>
    private void ClampMapPosition()
    {
        float scale = mapContent.localScale.x;

        float scaledContentWidth =
            mapContent.rect.width * scale;

        float scaledContentHeight =
            mapContent.rect.height * scale;

        float viewportWidth =
            mapViewport.rect.width;

        float viewportHeight =
            mapViewport.rect.height;

        /*
         * 地図の端が表示窓の端より内側へ入りすぎないように、
         * 移動できる最大距離を計算します。
         */
        float maxMoveX = Mathf.Max(
            0f,
            (scaledContentWidth - viewportWidth) * 0.5f
        );

        float maxMoveY = Mathf.Max(
            0f,
            (scaledContentHeight - viewportHeight) * 0.5f
        );

        Vector2 position = mapContent.anchoredPosition;

        position.x = Mathf.Clamp(
            position.x,
            -maxMoveX,
            maxMoveX
        );

        position.y = Mathf.Clamp(
            position.y,
            -maxMoveY,
            maxMoveY
        );

        mapContent.anchoredPosition = position;
    }

    /// <summary>
    /// 指定した場所がMapViewportの中央に来るように、
    /// 地図を自動で移動・拡大します。
    /// </summary>
    public void FocusOnTarget(
        RectTransform target,
        float targetScale,
        System.Action onComplete = null
    )
    {
        if (target == null)
        {
            Debug.LogWarning(
                "ヒント対象のRectTransformが設定されていません。"
            );

            return;
        }

        hintFocusSequence?.Kill();

        isInputLocked = true;
        isMouseDragging = false;
        isTouchDragging = false;

        float clampedScale =
            Mathf.Clamp(
                targetScale,
                minScale,
                maxScale
            );

        // 対象地点のMapContent内座標
        Vector2 targetLocalPosition =
            mapContent.InverseTransformPoint(
                target.position
            );

        // 対象地点をViewport中央へ移動する位置
        Vector2 desiredMapPosition =
            -targetLocalPosition * clampedScale;

        // 最終位置を移動可能範囲内へ制限する
        Vector2 previousPosition =
            mapContent.anchoredPosition;

        Vector3 previousScale =
            mapContent.localScale;

        SetScale(clampedScale);
        mapContent.anchoredPosition =
            desiredMapPosition;

        ClampMapPosition();

        Vector2 clampedMapPosition =
            mapContent.anchoredPosition;

        // Tween開始前の状態へ戻す
        mapContent.anchoredPosition =
            previousPosition;

        mapContent.localScale =
            previousScale;

        hintFocusSequence =
            DOTween.Sequence();

        hintFocusSequence.Append(
            mapContent
                .DOScale(
                    new Vector3(
                        clampedScale,
                        clampedScale,
                        1f
                    ),
                    hintFocusDuration
                )
                .SetEase(hintFocusEase)
        );

        hintFocusSequence.Join(
            mapContent
                .DOAnchorPos(
                    clampedMapPosition,
                    hintFocusDuration
                )
                .SetEase(hintFocusEase)
        );

        hintFocusSequence.OnComplete(() =>
        {
            ClampMapPosition();

            isInputLocked = false;

            onComplete?.Invoke();
        });

        hintFocusSequence.OnKill(() =>
        {
            isInputLocked = false;
        });

    }


    /// <summary>
    /// ヒントによる自動移動を途中終了する。
    /// </summary>
    public void CancelHintFocus()
    {
        hintFocusSequence?.Kill();
        hintFocusSequence = null;

        isInputLocked = false;
        isMouseDragging = false;
        isTouchDragging = false;

        ClampMapPosition();
    }

    /// <summary>
    /// 途中保存されていた地図の位置と倍率を復元する。
    /// </summary>
    public void RestoreMapView(
        Vector2 savedPosition,
        float savedScale
    )
    {
        if (mapContent == null)
        {
            Debug.LogWarning(
                "MapPanZoomController：" +
                "MapContentがないため復元できません。"
            );

            return;
        }

        // ヒントによる自動移動があれば終了
        CancelHintFocus();

        float clampedScale =
            Mathf.Clamp(
                savedScale,
                minScale,
                maxScale
            );

        SetScale(
            clampedScale
        );

        mapContent.anchoredPosition =
            savedPosition;

        // 保存位置が現在の画面範囲外なら内側へ収める
        ClampMapPosition();

        Debug.Log(
            $"地図表示を復元しました。 " +
            $"Position={mapContent.anchoredPosition}, " +
            $"Scale={clampedScale}"
        );
    }

    /// <summary>
    /// 地図を初期位置・初期倍率に戻します。
    /// 将来的にリセットボタンから呼び出せます。
    /// </summary>
    public void ResetMap()
    {
        mapContent.anchoredPosition = Vector2.zero;
        SetScale(minScale);
        ClampMapPosition();
    }

    private void OnDestroy()
    {
        hintFocusSequence?.Kill();
    }

}