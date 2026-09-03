using UnityEngine;

/// <summary>
/// Stage_Page / BackGroundに追加する広告用レイアウト。
/// 広告SDKを呼ばず、BannerAdControllerの高さを親の座標へ換算する。
/// BottomAreaとMiddleAreaの下端、内部の地図・カードは直接変更しない。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class StageBannerUILayout : MonoBehaviour
{
    [Header("BackGround直下のオブジェクト")]
    [SerializeField] private RectTransform adBannerArea;
    [SerializeField] private RectTransform buttonArea;
    [SerializeField] private RectTransform middleArea;

    [Header("親のUI座標での余白")]
    [Min(0f)][SerializeField] private float minimumAreaHeight = 100f;
    [Min(0f)][SerializeField] private float buttonGap = 10f;
    [Min(0f)][SerializeField] private float middleGap;
    [Tooltip("広告が画面上端より下に表示される場合の補正。通常は0。")]
    [Min(0f)][SerializeField] private float extraTopInsetPixels;

    private RectTransform layoutRoot;
    private BannerAdController banner;
    private float lastMeasuredPixels;
    private bool initialized;
    private float originalAdHeight;
    private Vector2 originalAdPosition;
    private Vector2 originalButtonPosition;
    private float originalMiddleTop;

    private void Start()
    {
        layoutRoot = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            StopWithError("Screen Space - OverlayのCanvas内で使ってください。");
            return;
        }
        if (!IsDirectChild(adBannerArea) || !IsDirectChild(buttonArea) ||
            !IsDirectChild(middleArea) || adBannerArea == buttonArea ||
            adBannerArea == middleArea || buttonArea == middleArea ||
            !IsTopAnchored(adBannerArea) || !IsTopAnchored(buttonArea) ||
            !Mathf.Approximately(middleArea.anchorMin.y, 0f) ||
            !Mathf.Approximately(middleArea.anchorMax.y, 1f))
        {
            StopWithError("3つの参照を指定してください。広告帯とボタン列は上固定・Pivot Y=1、MiddleAreaは上下Stretchが必要です。");
            return;
        }
        if (Quaternion.Angle(layoutRoot.rotation, Quaternion.identity) > 0.01f ||
            layoutRoot.lossyScale.x <= 0f || layoutRoot.lossyScale.y <= 0f)
        {
            StopWithError("BackGroundとその親の回転は0、Scaleは正の値にしてください。");
            return;
        }
        originalAdHeight = adBannerArea.rect.height;
        originalAdPosition = adBannerArea.anchoredPosition;
        originalButtonPosition = buttonArea.anchoredPosition;
        originalMiddleTop = middleArea.offsetMax.y;
        initialized = true;
        ApplyLayout();
    }

    private bool IsDirectChild(RectTransform rect)
    {
        return rect != null && rect.parent == transform &&
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
        if (adBannerArea == null || buttonArea == null || middleArea == null) return;
        if (banner == null && AdManager.Instance != null)
            banner = AdManager.Instance.GetComponent<BannerAdController>();
        if (banner != null && banner.BannerHeightPixels > 0f)
            lastMeasuredPixels = banner.BannerHeightPixels;

        // 親の位置・スケールとCanvas ScalerのExpandを含めて換算する。
        // 広告の一時非表示中は最後の高さを維持し、画面の跳ねを防ぐ。
        Vector2 screenBottom = new Vector2(Screen.width * 0.5f,
            Screen.height - lastMeasuredPixels - extraTopInsetPixels);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            layoutRoot, screenBottom, null, out Vector2 localBottom)) return;

        float height = Mathf.Max(minimumAreaHeight, layoutRoot.rect.yMax - localBottom.y);
        if (!Mathf.Approximately(adBannerArea.rect.height, height))
            adBannerArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        Vector2 adPosition = adBannerArea.anchoredPosition;
        adPosition.y = 0f;
        if (adBannerArea.anchoredPosition != adPosition) adBannerArea.anchoredPosition = adPosition;

        Vector2 buttonPosition = buttonArea.anchoredPosition;
        buttonPosition.y = -(height + buttonGap);
        if (buttonArea.anchoredPosition != buttonPosition) buttonArea.anchoredPosition = buttonPosition;

        Vector2 middleOffset = middleArea.offsetMax;
        middleOffset.y = -(height + buttonGap + buttonArea.rect.height + middleGap);
        if (middleArea.offsetMax != middleOffset) middleArea.offsetMax = middleOffset;
        // offsetMinには触れないので、MiddleAreaのBottom=110を維持する。
    }

    private void OnDisable()
    {
        if (!initialized) return;
        if (adBannerArea != null)
        {
            adBannerArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalAdHeight);
            adBannerArea.anchoredPosition = originalAdPosition;
        }
        if (buttonArea != null) buttonArea.anchoredPosition = originalButtonPosition;
        if (middleArea != null)
        {
            Vector2 offset = middleArea.offsetMax;
            offset.y = originalMiddleTop;
            middleArea.offsetMax = offset;
        }
    }

    private void StopWithError(string message)
    {
        Debug.LogError("[Stage Banner UI] " + message, this);
        enabled = false;
    }
}
