using DG.Tweening;
using UnityEngine;

/// <summary>
/// 正解したスロット位置で、
/// UIParticleを1回だけ再生する。
/// </summary>
public class CorrectDropEffectUI : MonoBehaviour
{
    [Header("画面全体のエフェクトレイヤー")]
    [SerializeField]
    private RectTransform effectLayer;

    [Header("正解エフェクト")]
    [SerializeField]
    private RectTransform effectRoot;

    [Header("再生終了後に非表示にするまでの時間")]
    [SerializeField]
    private float hideDelay = 1f;

    private ParticleSystem[] particles;
    private Tween hideTween;


    private void Awake()
    {
        if (effectLayer == null)
        {
            effectLayer =
                transform as RectTransform;
        }

        if (effectRoot != null)
        {
            particles =
                effectRoot.GetComponentsInChildren<ParticleSystem>(
                    true
                );

            effectRoot.gameObject.SetActive(false);
        }
    }


    /// <summary>
    /// 指定されたスロットの中央で再生する。
    /// </summary>
    public void PlayAtSlot(
        RectTransform slotRect,
        Camera eventCamera)
    {
        if (effectLayer == null ||
            effectRoot == null ||
            slotRect == null)
        {
            return;
        }

        Vector3 slotWorldCenter =
            slotRect.TransformPoint(
                slotRect.rect.center
            );

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                slotWorldCenter
            );

        bool converted =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                effectLayer,
                screenPosition,
                eventCamera,
                out Vector2 localPosition
            );

        if (!converted)
        {
            return;
        }

        PlayAtLocalPosition(
            localPosition
        );
    }


    /// <summary>
    /// エフェクトを移動して最初から再生する。
    /// </summary>
    private void PlayAtLocalPosition(
        Vector2 localPosition)
    {
        hideTween?.Kill();

        effectRoot.gameObject.SetActive(true);
        effectRoot.anchoredPosition =
            localPosition;

        if (particles == null ||
            particles.Length == 0)
        {
            particles =
                effectRoot.GetComponentsInChildren<ParticleSystem>(
                    true
                );
        }

        foreach (ParticleSystem particle
                 in particles)
        {
            if (particle == null)
            {
                continue;
            }

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            particle.Play(true);
        }

        hideTween =
            DOVirtual.DelayedCall(
                hideDelay,
                HideImmediately
            );
    }


    private void HideImmediately()
    {
        if (particles != null)
        {
            foreach (ParticleSystem particle
                     in particles)
            {
                if (particle == null)
                {
                    continue;
                }

                particle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }

        if (effectRoot != null)
        {
            effectRoot.gameObject.SetActive(false);
        }

        hideTween = null;
    }


    private void OnDestroy()
    {
        hideTween?.Kill();
    }
}