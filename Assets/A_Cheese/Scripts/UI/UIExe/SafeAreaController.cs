using UnityEngine;

/// <summary>
/// 端末のSafe AreaにRectTransformを合わせる。
/// ノッチやホームインジケーターへのUI被りを防ぐ。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaController : MonoBehaviour
{
    private RectTransform rectTransform;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        ApplySafeArea();
    }

    private void Update()
    {
        // 画面サイズやSafe Areaが変わった時だけ再設定
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;

        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(
            Screen.width,
            Screen.height
        );
    }
}