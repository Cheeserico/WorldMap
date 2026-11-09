
using System;
using UnityEngine;
using DG.Tweening;

public class AirplanePageEffect : MonoBehaviour
{
    [Header("飛行機")]
    [SerializeField]
    private RectTransform airplane;

    [Header("飛行設定")]
    [SerializeField]
    private float flightDuration = 1.1f;

    [SerializeField]
    private float flightY = 30f;

    [SerializeField]
    private float outsideMargin = 100f;

    [Header("発進前の後退")]
    [SerializeField]
    private float backDistance = 80f;

    [SerializeField]
    private float backDuration = 0.25f;

    [SerializeField]
    private float pauseDuration = 0.08f;

    [Header("遠近感")]
    [SerializeField]
    private float startScale = 1f;

    [SerializeField]
    private float backScale = 0.75f;

    [SerializeField]
    private float flyScale = 1.15f;

    private RectTransform parentRect;
    private Sequence flightSequence;

    private void Awake()
    {
        if (airplane == null)
            airplane = GetComponent<RectTransform>();

        parentRect = airplane.parent as RectTransform;

        airplane.gameObject.SetActive(false);
    }

    public void Play(Action onComplete = null)
    {
        if (airplane == null || parentRect == null)
            return;

        // 前回のアニメーションを停止
        flightSequence?.Kill();

        float halfWidth = parentRect.rect.width * 0.5f;

        float airplaneHalfWidth =
            airplane.rect.width * 0.5f;

        // 左端に少し見える位置
        float startX =
            -halfWidth + airplaneHalfWidth * 0.6f + 400f;

        // 右側の画面外
        float endX =
            halfWidth +
            airplaneHalfWidth * Mathf.Max(startScale, flyScale)
            + outsideMargin;

        airplane.anchoredPosition =
            new Vector2(startX, flightY);

        airplane.localScale =
            Vector3.one * startScale;

        airplane.gameObject.SetActive(true);

        flightSequence = DOTween.Sequence()
            .SetUpdate(true);

        // ① 後退しながら小さくなる
        flightSequence.Append(
            airplane.DOAnchorPosX(
                startX - backDistance,
                backDuration
            ).SetEase(Ease.OutCubic)
        );

        flightSequence.Join(
            airplane.DOScale(
                backScale,
                backDuration
            ).SetEase(Ease.OutCubic)
        );

        // ② 一瞬ためる
        flightSequence.AppendInterval(
            pauseDuration
        );

        // ③ 右へ加速して飛行
        flightSequence.Append(
            airplane.DOAnchorPosX(
                endX,
                flightDuration
            ).SetEase(Ease.InCubic)
        );

        // 飛び出すときに大きくなる
        flightSequence.Join(
            airplane.DOScale(
                flyScale,
                flightDuration * 0.55f
            ).SetEase(Ease.OutCubic)
        );

        // ④ 終了後に非表示
        flightSequence.OnComplete(() =>
        {
            airplane.gameObject.SetActive(false);
            flightSequence = null;

            // 飛行機の演出が終わったら次の処理へ
            onComplete?.Invoke();
        });
    }

    private void OnDestroy()
    {
        flightSequence?.Kill();
    }
}
