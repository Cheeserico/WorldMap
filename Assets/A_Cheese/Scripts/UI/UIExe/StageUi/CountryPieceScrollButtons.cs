using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 国ピース一覧を左右のボタンでスクロールする。
/// </summary>
public class CountryPieceScrollButtons : MonoBehaviour
{
    [Header("国ピースのScroll Rect")]
    [SerializeField]
    private ScrollRect pieceScrollRect;

    [Header("1回で動かす距離")]
    [Tooltip("Viewportの横幅に対する移動割合")]
    [SerializeField]
    [Range(0.1f, 1f)]
    private float scrollAmount = 0.8f;

    [Header("スクロール演出")]
    [SerializeField]
    private float scrollDuration = 0.25f;

    [SerializeField]
    private Ease scrollEase = Ease.OutCubic;

    private Tween scrollTween;

    /// <summary>
    /// 左矢印から呼び出す。
    /// </summary>
    public void ScrollLeft()
    {
        Scroll(-1f);
    }

    /// <summary>
    /// 右矢印から呼び出す。
    /// </summary>
    public void ScrollRight()
    {
        Scroll(1f);
    }

    private void Scroll(float direction)
    {
        if (pieceScrollRect == null ||
            pieceScrollRect.content == null ||
            pieceScrollRect.viewport == null)
        {
            Debug.LogWarning(
                "CountryPieceScrollButtons：" +
                "Scroll Rectの参照が設定されていません。",
                this
            );

            return;
        }

        // Layout Groupなどのサイズ計算を確定させる
        Canvas.ForceUpdateCanvases();

        float contentWidth =
            pieceScrollRect.content.rect.width;

        float viewportWidth =
            pieceScrollRect.viewport.rect.width;

        float scrollableWidth =
            contentWidth - viewportWidth;

        // 一覧全体がViewport内に収まっている
        if (scrollableWidth <= 0f)
        {
            return;
        }

        float moveDistance =
            viewportWidth * scrollAmount;

        float normalizedAmount =
            moveDistance / scrollableWidth;

        float currentPosition =
            pieceScrollRect.horizontalNormalizedPosition;

        float targetPosition =
            Mathf.Clamp01(
                currentPosition +
                normalizedAmount * direction
            );

        scrollTween?.Kill();

        scrollTween =
            DOTween.To(
                () =>
                    pieceScrollRect
                        .horizontalNormalizedPosition,
                value =>
                    pieceScrollRect
                        .horizontalNormalizedPosition =
                        value,
                targetPosition,
                scrollDuration
            )
            .SetEase(scrollEase)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        scrollTween?.Kill();
        scrollTween = null;
    }
}