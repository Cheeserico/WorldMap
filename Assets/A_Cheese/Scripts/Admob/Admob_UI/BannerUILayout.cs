using UnityEngine;

/// <summary>
/// Overlay Canvas直下の広告帯・ヘッダー・地図上端を調整する。
/// 広告SDKや端末IDにはアクセスせず、BannerAdControllerの計測値を使う。
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
    [Tooltip("広告の読み込み前や広告が出ない場合に確保する高さ。")]
    [Min(0f)][SerializeField] private float minimumAreaHeight = 100f;
    [Tooltip("広告帯とヘッダーの間の余白。")]
    [Min(0f)][SerializeField] private float headerGap = 12f;
    [Tooltip("広告が画面上端から離れて表示される場合の補正。通常は0。")]
    [Min(0f)][SerializeField] private float extraTopInsetPixels;

    private Canvas targetCanvas;
    private BannerAdController banner;
    private float lastMeasuredHeightPixels;
    private bool initialized;
    private Vector2 originalAdPosition;
    private float originalAdHeight;
    private Vector2 originalHeaderPosition;
    private float originalMapTopOffset;

    private void Start()
    {
        targetCanvas = GetComponent<Canvas>();
        if (!targetCanvas.isRootCanvas ||
            targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ||
            adArea == null || headerUI == null || mapViewport == null)
        {
            StopWithError("OverlayのルートCanvasと、3つの参照を設定してください。");
            return;
        }

        if (adArea == headerUI || adArea == mapViewport || headerUI == mapViewport ||
            !IsDirectUnscaledChild(adArea) || !IsDirectUnscaledChild(headerUI) ||
            !IsDirectUnscaledChild(mapViewport))
        {
            StopWithError("参照にはCanvas直下の別々のオブジェクトを指定してください。Scaleは1、Rotationは0が必要です。");
            return;
        }

        if (!IsTopAnchored(adArea) || !IsTopAnchored(headerUI) ||
            !Mathf.Approximately(mapViewport.anchorMin.y, 0f) ||
            !Mathf.Approximately(mapViewport.anchorMax.y, 1f))
        {
            StopWithError("広告帯・ヘッダーは上固定（Pivot Y=1）、地図は上下Stretchにしてください。");
            return;
        }

        originalAdPosition = adArea.anchoredPosition;
        originalAdHeight = adArea.rect.height;
        originalHeaderPosition = headerUI.anchoredPosition;
        originalMapTopOffset = mapViewport.offsetMax.y;
        initialized = true;
        ApplyLayout();
    }

    private bool IsDirectUnscaledChild(RectTransform rect)
    {
        return rect.parent == transform &&
            (rect.localScale - Vector3.one).sqrMagnitude < 0.0001f &&
            Quaternion.Angle(rect.localRotation, Quaternion.identity) < 0.01f;
    }

    private static bool IsTopAnchored(RectTransform rect)
    {
        return Mathf.Approximately(rect.anchorMin.y, 1f) &&
            Mathf.Approximately(rect.anchorMax.y, 1f) &&
            Mathf.Approximately(rect.pivot.y, 1f);
    }

    private void LateUpdate()
    {
        if (initialized) ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (adArea == null || headerUI == null || mapViewport == null) return;

        if (banner == null && AdManager.Instance != null)
            banner = AdManager.Instance.GetComponent<BannerAdController>();

        if (banner != null && banner.BannerHeightPixels > 0f)
            lastMeasuredHeightPixels = banner.BannerHeightPixels;

        // 一時的な非表示・再読み込みでは地図が上下に跳ねないよう余白を維持する。
        float scale = targetCanvas.scaleFactor;
        if (scale <= 0f) return;
        float height = Mathf.Max(minimumAreaHeight,
            (lastMeasuredHeightPixels + extraTopInsetPixels) / scale);

        if (!Mathf.Approximately(adArea.rect.height, height))
            adArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        Vector2 adPosition = adArea.anchoredPosition;
        adPosition.y = 0f;
        if (adArea.anchoredPosition != adPosition) adArea.anchoredPosition = adPosition;

        Vector2 headerPosition = headerUI.anchoredPosition;
        headerPosition.y = -height - headerGap;
        if (headerUI.anchoredPosition != headerPosition) headerUI.anchoredPosition = headerPosition;

        // offsetMin（Bottom=220）と左右の値は変更しない。
        Vector2 mapOffset = mapViewport.offsetMax;
        mapOffset.y = -height;
        if (mapViewport.offsetMax != mapOffset) mapViewport.offsetMax = mapOffset;
    }

    private void OnDisable()
    {
        if (!initialized) return;
        if (adArea != null)
        {
            adArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalAdHeight);
            adArea.anchoredPosition = originalAdPosition;
        }
        if (headerUI != null) headerUI.anchoredPosition = originalHeaderPosition;
        if (mapViewport != null)
        {
            Vector2 offset = mapViewport.offsetMax;
            offset.y = originalMapTopOffset;
            mapViewport.offsetMax = offset;
        }
    }

    private void StopWithError(string message)
    {
        Debug.LogError("[Banner UI] " + message, this);
        enabled = false;
    }
}
