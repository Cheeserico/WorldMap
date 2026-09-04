using UnityEngine;

/// <summary>
/// Overlay Canvas直下の広告帯・ヘッダー・地図上端を調整する。
/// 広告表示前に予定高さを確保し、一度確定した高さは広告更新で変更しない。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class BannerUILayout : MonoBehaviour
{
    [Header("このCanvas直下のRectTransform")]
    [SerializeField] private RectTransform adArea;
    [SerializeField] private RectTransform headerUI;
    [SerializeField] private RectTransform mapViewport;

    [Header("余白（Canvas上のUI単位）")]
    [Tooltip("予定高さを取得できない場合にも確保する最小の高さ。")]
    [Min(0f)]
    [SerializeField] private float minimumAreaHeight = 100f;

    [Tooltip("広告帯とヘッダーの間の余白。")]
    [Min(0f)]
    [SerializeField] private float headerGap = 12f;

    [Tooltip("広告が画面上端から離れて表示される場合の補正。通常は0。")]
    [Min(0f)]
    [SerializeField] private float extraTopInsetPixels;

    private Canvas targetCanvas;
    private BannerAdController banner;

    private bool initialized;
    private bool bannerHeightLocked;

    private Vector2 originalAdPosition;
    private float originalAdHeight;
    private Vector2 originalHeaderPosition;
    private float originalMapTopOffset;


    private void Start()
    {
        targetCanvas = GetComponent<Canvas>();

        if (!targetCanvas.isRootCanvas ||
            targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ||
            adArea == null ||
            headerUI == null ||
            mapViewport == null)
        {
            StopWithError(
                "OverlayのルートCanvasと、" +
                "3つの参照を設定してください。"
            );

            return;
        }

        if (adArea == headerUI ||
            adArea == mapViewport ||
            headerUI == mapViewport ||
            !IsDirectUnscaledChild(adArea) ||
            !IsDirectUnscaledChild(headerUI) ||
            !IsDirectUnscaledChild(mapViewport))
        {
            StopWithError(
                "参照にはCanvas直下の別々のオブジェクトを" +
                "指定してください。Scaleは1、Rotationは0が必要です。"
            );

            return;
        }

        if (!IsTopAnchored(adArea) ||
            !IsTopAnchored(headerUI) ||
            !Mathf.Approximately(mapViewport.anchorMin.y, 0f) ||
            !Mathf.Approximately(mapViewport.anchorMax.y, 1f))
        {
            StopWithError(
                "広告帯・ヘッダーは上固定（Pivot Y=1）、" +
                "地図は上下Stretchにしてください。"
            );

            return;
        }

        originalAdPosition = adArea.anchoredPosition;
        originalAdHeight = adArea.rect.height;
        originalHeaderPosition = headerUI.anchoredPosition;
        originalMapTopOffset = mapViewport.offsetMax.y;

        initialized = true;

        // まず最小高さを即座に適用する。
        ApplyLayout(minimumAreaHeight);

        // BannerAdControllerは広告読み込み前に予定高さを持っている。
        TryApplyAndLockBannerHeight();
    }


    private void LateUpdate()
    {
        // 実行順の違いに備えて、高さを取得できるまでだけ確認する。
        // 確定後は毎フレームのレイアウト変更を行わない。
        if (initialized && !bannerHeightLocked)
        {
            TryApplyAndLockBannerHeight();
        }
    }


    private void TryApplyAndLockBannerHeight()
    {
        if (banner == null && AdManager.Instance != null)
        {
            banner = AdManager.Instance
                .GetComponent<BannerAdController>();
        }

        if (banner == null ||
            banner.BannerHeightPixels <= 0f)
        {
            return;
        }

        float scale = targetCanvas.scaleFactor;

        if (scale <= 0f)
        {
            return;
        }

        float reservedHeight =
            (banner.BannerHeightPixels +
             extraTopInsetPixels) / scale;

        // サブピクセル差をなくすため、Canvas UI単位でも切り上げる。
        reservedHeight = Mathf.Ceil(reservedHeight);

        float height = Mathf.Max(
            minimumAreaHeight,
            reservedHeight
        );

        ApplyLayout(height);
        bannerHeightLocked = true;

        Debug.Log(
            "[Banner UI] 広告表示前に高さを固定: " +
            height + " UI units",
            this
        );
    }


    private void ApplyLayout(float height)
    {
        if (adArea == null ||
            headerUI == null ||
            mapViewport == null)
        {
            return;
        }

        if (Mathf.Abs(adArea.rect.height - height) > 0.01f)
        {
            adArea.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height
            );
        }

        Vector2 adPosition = adArea.anchoredPosition;
        adPosition.y = 0f;

        if (adArea.anchoredPosition != adPosition)
        {
            adArea.anchoredPosition = adPosition;
        }

        Vector2 headerPosition =
            headerUI.anchoredPosition;

        headerPosition.y = -height - headerGap;

        if (headerUI.anchoredPosition != headerPosition)
        {
            headerUI.anchoredPosition = headerPosition;
        }

        // Bottomと左右の値は変更しない。
        Vector2 mapOffset = mapViewport.offsetMax;
        mapOffset.y = -height;

        if (mapViewport.offsetMax != mapOffset)
        {
            mapViewport.offsetMax = mapOffset;
        }
    }


    private bool IsDirectUnscaledChild(RectTransform rect)
    {
        return rect.parent == transform &&
            (rect.localScale - Vector3.one).sqrMagnitude < 0.0001f &&
            Quaternion.Angle(
                rect.localRotation,
                Quaternion.identity
            ) < 0.01f;
    }


    private static bool IsTopAnchored(RectTransform rect)
    {
        return Mathf.Approximately(rect.anchorMin.y, 1f) &&
            Mathf.Approximately(rect.anchorMax.y, 1f) &&
            Mathf.Approximately(rect.pivot.y, 1f);
    }


    private void OnDisable()
    {
        if (!initialized)
        {
            return;
        }

        if (adArea != null)
        {
            adArea.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                originalAdHeight
            );

            adArea.anchoredPosition = originalAdPosition;
        }

        if (headerUI != null)
        {
            headerUI.anchoredPosition =
                originalHeaderPosition;
        }

        if (mapViewport != null)
        {
            Vector2 offset = mapViewport.offsetMax;
            offset.y = originalMapTopOffset;
            mapViewport.offsetMax = offset;
        }
    }


    private void StopWithError(string message)
    {
        Debug.LogError(
            "[Banner UI] " + message,
            this
        );

        enabled = false;
    }
}
