
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;


/// <summary>
/// ステージ開始時の飛行機演出を管理する。
/// 演出終了後にGameSceneへ移動する。
/// </summary>
public class StageStartEffectController : MonoBehaviour
{
    public static StageStartEffectController Instance
    {
        get;
        private set;
    }

    [Header("飛行機エフェクト")]
    [SerializeField]
    private AirplanePageEffect airplaneEffect;

    [Header("飛行機と同時に再生するエフェクト")]
    [SerializeField]
    private ParticleSystem[] startEffects;

    [Header("背景暗転")]
    [SerializeField]
    private Image darkOverlay;

    [SerializeField, Range(0f, 1f)]
    private float darkAlpha = 0.4f;

    [SerializeField]
    private float darkDuration = 0.2f;

    // 連続操作による二重開始を防ぐ
    private bool isStarting;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 飛行機演出後にシーン移動する。
    /// </summary>
    public void PlayAndLoad(string sceneName)
    {
        if (isStarting)
        {
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning(
                "StageStartEffectController：" +
                "Scene名が設定されていません。"
            );

            return;
        }

        isStarting = true;

        // 飛行機が未登録なら直接移動
        if (airplaneEffect == null)
        {
            SceneTransitionManager.Load(sceneName);
            return;
        }

        // 背景を暗くする
        PlayDarkOverlay();

        // 飛行機と同時にエフェクトを再生
        PlayStartEffects();

        airplaneEffect.Play(() =>
        {
            SceneTransitionManager.Load(sceneName);
        });
    }

    private void PlayStartEffects()
    {
        if (startEffects == null)
        {
            return;
        }

        foreach (ParticleSystem effect in startEffects)
        {
            if (effect == null)
            {
                continue;
            }

            effect.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            effect.Play(true);
        }
    }

    private void PlayDarkOverlay()
    {
        if (darkOverlay == null)
        {
            return;
        }

        darkOverlay.DOKill();

        Color color = darkOverlay.color;
        color.a = 0f;
        darkOverlay.color = color;

        darkOverlay.DOFade(darkAlpha, darkDuration)
            .SetEase(Ease.OutQuad);
    }

}
