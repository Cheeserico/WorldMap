using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ステージ選択用。基準サイズを維持し、地図・カード・線をまとめて縮小する。
/// MapContentに付ける。親はページの表示領域に合わせてStretchにする。
/// 同じオブジェクトのAspectRatioFitterやサイズを変更する処理とは併用しない。
/// ゲーム画面の移動・ズーム用MapContentには付けない。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class StageMapFitToParent : MonoBehaviour
{
    [Header("配置を作ったときのサイズ")]
    [SerializeField] private Vector2 referenceSize = new Vector2(1520f, 760f);

    [Header("基準枠の外へ出ているカード用の余白（片側分）")]
    [Tooltip("カードが基準枠からはみ出す場合、その分を指定。基準座標は変えずに全体を小さくします。")]
    [SerializeField] private Vector2 contentMargin = Vector2.zero;

    [Header("親の内側に確保する余白（片側分）")]
    [SerializeField] private Vector2 viewportPadding = new Vector2(16f, 16f);

    [Tooltip("通常は1。大画面でも基準サイズより大きくしません。")]
    [SerializeField, Min(0.01f)] private float maximumScale = 1f;

    private RectTransform target;
    private AspectRatioFitter aspectFitter;

    private void OnEnable()
    {
        target = GetComponent<RectTransform>();
        aspectFitter = GetComponent<AspectRatioFitter>();
        if (aspectFitter != null && aspectFitter.enabled)
            Debug.LogWarning("StageMapFitToParent：同じオブジェクトのAspect Ratio Fitterを無効にしてください。", this);
        ApplyLayout();
    }

    // OnValidate中にはRectTransformを変更しない。
    // 非表示ページも、再度有効になった際にOnEnableで更新する。
    private void LateUpdate()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (target == null || (aspectFitter != null && aspectFitter.enabled))
            return;

        RectTransform parent = target.parent as RectTransform;
        if (parent == null || referenceSize.x <= 0f || referenceSize.y <= 0f)
            return;

        float width = parent.rect.width - 2f * Mathf.Max(0f, viewportPadding.x);
        float height = parent.rect.height - 2f * Mathf.Max(0f, viewportPadding.y);
        if (width <= 0f || height <= 0f)
            return;

        float totalWidth = referenceSize.x + 2f * Mathf.Max(0f, contentMargin.x);
        float totalHeight = referenceSize.y + 2f * Mathf.Max(0f, contentMargin.y);
        float scale = Mathf.Min(width / totalWidth, height / totalHeight);
        scale = Mathf.Min(scale, Mathf.Max(0.01f, maximumScale));

        Vector2 center = new Vector2(0.5f, 0.5f);
        if (target.anchorMin != center) target.anchorMin = center;
        if (target.anchorMax != center) target.anchorMax = center;
        if (target.pivot != center) target.pivot = center;
        if (target.sizeDelta != referenceSize) target.sizeDelta = referenceSize;
        if (target.anchoredPosition3D != Vector3.zero) target.anchoredPosition3D = Vector3.zero;
        Vector3 desiredScale = new Vector3(scale, scale, 1f);
        if (target.localScale != desiredScale) target.localScale = desiredScale;
    }
}
