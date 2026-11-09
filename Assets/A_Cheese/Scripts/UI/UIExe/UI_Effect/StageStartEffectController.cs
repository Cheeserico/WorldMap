
using UnityEngine;

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
}
