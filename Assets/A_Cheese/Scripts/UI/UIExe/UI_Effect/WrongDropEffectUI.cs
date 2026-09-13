using DG.Tweening;
using UnityEngine;

/// <summary>
/// 不正解時に、指を離した画面位置で
/// UIParticleを1回だけ再生する。
/// </summary>
public class WrongDropEffectUI : MonoBehaviour
{
    [Header("画面全体のエフェクトレイヤー")]
    [SerializeField]
    private RectTransform effectLayer;

    [Header("不正解エフェクト")]
    [SerializeField]
    private RectTransform effectRoot;

    [Header("再生終了後に非表示にするまでの時間")]
    [SerializeField]
    private float hideDelay = 0.8f;

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
    /// 指を離した画面位置でエフェクトを再生する。
    /// </summary>
    public void Play(
        Vector2 screenPosition,
        Camera eventCamera)
    {
        if (effectLayer == null ||
            effectRoot == null)
        {
            return;
        }

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