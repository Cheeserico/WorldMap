
using System;
using UnityEngine;
using DG.Tweening;

public class CloudGatherEffect : MonoBehaviour
{
    [Header("四隅の雲（左上・右上・左下・右下）")]
    [SerializeField] private RectTransform[] clouds = new RectTransform[4];

    [Header("移動時間")]
    [SerializeField] private float gatherDuration = 0.35f;
    [SerializeField] private float holdDuration = 0.12f;
    [SerializeField] private float escapeDuration = 0.25f;

    [Header("中央に集まる位置の調整")]
    [SerializeField]
    private Vector2[] centerOffsets = new Vector2[4]
    {
        new Vector2(-160, 90),
        new Vector2(160, 90),
        new Vector2(-160, -90),
        new Vector2(160, -90)
    };

    private Vector2[] originalPositions;
    private Sequence sequence;
    private bool isPlaying;

    private void Awake()
    {
        originalPositions = new Vector2[clouds.Length];

        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] != null)
            {
                originalPositions[i] = clouds[i].anchoredPosition;
            }
        }
    }


    public void Play(
        Action onCovered = null,
        Action onComplete = null)
    {
        if (isPlaying) return;

        if (clouds == null || clouds.Length != 4 ||
            centerOffsets == null || centerOffsets.Length != 4)
        {
            onCovered?.Invoke();
            onComplete?.Invoke();
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (clouds[i] == null)
            {
                onCovered?.Invoke();
                onComplete?.Invoke();
                return;
            }
        }

        isPlaying = true;
        sequence?.Kill();

        sequence = DOTween.Sequence().SetUpdate(true);

        // ① 四隅から中央へ集合
        for (int i = 0; i < 4; i++)
        {
            sequence.Join(
                clouds[i].DOAnchorPos(
                    centerOffsets[i],
                    gatherDuration
                ).SetEase(Ease.OutCubic)
            );
        }

        // ② 中央に集まった瞬間にページ変更
        sequence.AppendCallback(() =>
        {
            onCovered?.Invoke();
        });

        // ③ 一瞬停止
        sequence.AppendInterval(holdDuration);

        // ④ 四隅へ素早く退散
        for (int i = 0; i < 4; i++)
        {
            sequence.Join(
                clouds[i].DOAnchorPos(
                    originalPositions[i],
                    escapeDuration
                ).SetEase(Ease.InCubic)
            );
        }

        sequence.OnComplete(() =>
        {
            isPlaying = false;
            sequence = null;
            onComplete?.Invoke();
        });
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }
}
