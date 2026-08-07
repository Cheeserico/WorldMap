using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public enum BGMType
{
    Title,
    Game,
    Result
}

public enum SEType
{
    Correct,
    Wrong,
    Button,
    Clear,
    Coin
}

/// <summary>
/// InspectorでBGMの種類とAudioClipを登録するためのデータ
/// </summary>
[Serializable]
public class BGMData
{
    public BGMType type;
    public AudioClip clip;
}

/// <summary>
/// InspectorでSEの種類とAudioClipを登録するためのデータ
/// </summary>
[Serializable]
public class SEData
{
    public SEType type;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    // ==================================================
    // Singleton
    // ==================================================

    public static SoundManager Instance { get; private set; }

    // ==================================================
    // Inspector設定
    // ==================================================

    [Header("Audio Source")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("BGM一覧")]
    [SerializeField] private BGMData[] bgmList;

    [Header("SE一覧")]
    [SerializeField] private SEData[] seList;

    [Header("初期音量")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultBgmVolume = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultSeVolume = 1f;

    [Header("BGMフェード設定")]
    [Min(0f)]
    [SerializeField] private float defaultFadeDuration = 1f;

    [Header("ゲーム開始時")]
    [SerializeField] private bool playBgmOnStart = true;

    [SerializeField] private BGMType startBgmType = BGMType.Game;

    [Header("ポーズ時のBGM設定")]

    [Tooltip("ポーズ中に通常音量の何割まで下げるか")]
    [SerializeField]
    [Range(0f, 1f)]
    private float pauseBGMVolumeRate = 0.2f;

    [Tooltip("ポーズ時のBGMフェード時間")]
    [SerializeField]
    [Min(0f)]
    private float pauseBGMFadeDuration = 0.3f;

    // ポーズ直前の実際のBGM音量
    private float bgmVolumeBeforePause;

    // ポーズ用フェードが実行済みか
    private bool isBgmPaused;


    // ==================================================
    // Dictionary
    // ==================================================

    private readonly Dictionary<BGMType, AudioClip> bgmDictionary =
        new Dictionary<BGMType, AudioClip>();

    private readonly Dictionary<SEType, AudioClip> seDictionary =
        new Dictionary<SEType, AudioClip>();

    // ==================================================
    // 現在の設定
    // ==================================================

    private float bgmVolume;
    private float seVolume;

    private bool isBgmMuted;
    private bool isSeMuted;

    private BGMType? currentBgmType;

    private Tween bgmFadeTween;

    // ==================================================
    // 外部取得用
    // ==================================================

    public float BgmVolume => bgmVolume;
    public float SeVolume => seVolume;

    public bool IsBgmMuted => isBgmMuted;
    public bool IsSeMuted => isSeMuted;

    public bool IsBgmPlaying =>
        bgmSource != null && bgmSource.isPlaying;

    public BGMType? CurrentBgmType => currentBgmType;

    // ==================================================
    // Unityイベント
    // ==================================================

    private void Awake()
    {
        InitializeSingleton();

        if (Instance != this)
        {
            return;
        }

        CreateAudioDictionaries();
        LoadSoundSettings();
        ApplyAllSoundSettings();
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        if (playBgmOnStart)
        {
            PlayBGM(startBgmType, defaultFadeDuration);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            bgmFadeTween?.Kill();
            Instance = null;
        }
    }

    // ==================================================
    // 初期化
    // ==================================================

    /// <summary>
    /// SoundManagerを1つだけ存在させる
    /// </summary>
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Inspectorの配列をDictionaryへ登録する
    /// </summary>
    private void CreateAudioDictionaries()
    {
        bgmDictionary.Clear();
        seDictionary.Clear();

        RegisterBgmClips();
        RegisterSeClips();
    }

    private void RegisterBgmClips()
    {
        if (bgmList == null)
        {
            return;
        }

        foreach (BGMData data in bgmList)
        {
            if (data == null)
            {
                continue;
            }

            if (data.clip == null)
            {
                Debug.LogWarning(
                    $"SoundManager：BGM「{data.type}」のAudioClipが登録されていません。",
                    this
                );

                continue;
            }

            if (bgmDictionary.ContainsKey(data.type))
            {
                Debug.LogWarning(
                    $"SoundManager：BGM「{data.type}」が重複登録されています。",
                    this
                );

                continue;
            }

            bgmDictionary.Add(data.type, data.clip);
        }
    }

    private void RegisterSeClips()
    {
        if (seList == null)
        {
            return;
        }

        foreach (SEData data in seList)
        {
            if (data == null)
            {
                continue;
            }

            if (data.clip == null)
            {
                Debug.LogWarning(
                    $"SoundManager：SE「{data.type}」のAudioClipが登録されていません。",
                    this
                );

                continue;
            }

            if (seDictionary.ContainsKey(data.type))
            {
                Debug.LogWarning(
                    $"SoundManager：SE「{data.type}」が重複登録されています。",
                    this
                );

                continue;
            }

            seDictionary.Add(data.type, data.clip);
        }
    }

    // ==================================================
    // BGM再生
    // ==================================================

    /// <summary>
    /// 指定したBGMをフェード付きで再生する
    /// </summary>
    public void PlayBGM(BGMType type)
    {
        PlayBGM(type, defaultFadeDuration);
    }

    /// <summary>
    /// 指定したBGMを指定秒数でフェード再生する
    /// </summary>
    public void PlayBGM(BGMType type, float fadeDuration)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning(
                "SoundManager：BGM用AudioSourceが登録されていません。",
                this
            );

            return;
        }

        if (!bgmDictionary.TryGetValue(type, out AudioClip clip))
        {
            Debug.LogWarning(
                $"SoundManager：BGM「{type}」が登録されていません。",
                this
            );

            return;
        }

        // 同じBGMがすでに再生中なら再生し直さない
        if (currentBgmType == type &&
            bgmSource.clip == clip &&
            bgmSource.isPlaying)
        {
            return;
        }

        fadeDuration = Mathf.Max(0f, fadeDuration);

        bgmFadeTween?.Kill();

        // BGMが流れていない場合は、そのままフェードイン
        if (!bgmSource.isPlaying || bgmSource.clip == null)
        {
            StartNewBGM(type, clip, fadeDuration);
            return;
        }

        // 現在のBGMをフェードアウトしてから切り替える
        bgmFadeTween = bgmSource
            .DOFade(0f, fadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                StartNewBGM(type, clip, fadeDuration);
            });
    }

    /// <summary>
    /// 新しいBGMの再生を開始する
    /// </summary>
    private void StartNewBGM(
        BGMType type,
        AudioClip clip,
        float fadeDuration)
    {
        bgmFadeTween?.Kill();

        currentBgmType = type;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float targetVolume = GetAppliedBgmVolume();

        if (fadeDuration <= 0f)
        {
            bgmSource.volume = targetVolume;
            return;
        }

        bgmFadeTween = bgmSource
            .DOFade(targetVolume, fadeDuration)
            .SetUpdate(true);
    }

    /// <summary>
    /// BGMをフェードアウトして停止する
    /// </summary>
    public void StopBGM()
    {
        StopBGM(defaultFadeDuration);
    }

    /// <summary>
    /// BGMを指定秒数でフェードアウトして停止する
    /// </summary>
    public void StopBGM(float fadeDuration)
    {
        if (bgmSource == null)
        {
            return;
        }

        fadeDuration = Mathf.Max(0f, fadeDuration);

        bgmFadeTween?.Kill();

        if (fadeDuration <= 0f)
        {
            StopBgmImmediately();
            return;
        }

        bgmFadeTween = bgmSource
            .DOFade(0f, fadeDuration)
            .SetUpdate(true)
            .OnComplete(StopBgmImmediately);
    }

    /// <summary>
    /// BGMを即座に停止する
    /// </summary>
    public void StopBgmImmediately()
    {
        bgmFadeTween?.Kill();

        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            bgmSource.volume = GetAppliedBgmVolume();
        }

        currentBgmType = null;
    }

    /// <summary>
    /// BGMを一時停止する
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Pause();
    }

    /// <summary>
    /// 一時停止したBGMを再開する
    /// </summary>
    public void ResumeBGM()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.UnPause();
    }

    // ==================================================
    // SE再生
    // ==================================================

    /// <summary>
    /// 指定したSEを再生する
    /// </summary>
    public void PlaySE(SEType type)
    {
        PlaySE(type, 1f);
    }

    /// <summary>
    /// 指定したSEを個別音量付きで再生する
    /// volumeScaleは0～1
    /// </summary>
    public void PlaySE(SEType type, float volumeScale)
    {
        if (seSource == null)
        {
            Debug.LogWarning(
                "SoundManager：SE用AudioSourceが登録されていません。",
                this
            );

            return;
        }

        if (isSeMuted)
        {
            return;
        }

        if (!seDictionary.TryGetValue(type, out AudioClip clip))
        {
            Debug.LogWarning(
                $"SoundManager：SE「{type}」が登録されていません。",
                this
            );

            return;
        }

        volumeScale = Mathf.Clamp01(volumeScale);

        // seSource.volumeに基本音量を設定しているため、
        // volumeScaleには個別の倍率だけを渡す
        seSource.PlayOneShot(clip, volumeScale);
    }

    /// <summary>
    /// 現在再生しているSEをすべて停止する
    /// </summary>
    public void StopAllSE()
    {
        if (seSource == null)
        {
            return;
        }

        seSource.Stop();
    }

    // ==================================================
    // 音量
    // ==================================================

    /// <summary>
    /// BGM音量を変更して保存する
    /// </summary>
    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        ApplyBgmVolume();
        SaveSoundSettings();
    }

    /// <summary>
    /// SE音量を変更して保存する
    /// </summary>
    public void SetSeVolume(float volume)
    {
        seVolume = Mathf.Clamp01(volume);

        ApplySeVolume();
        SaveSoundSettings();
    }

    /// <summary>
    /// BGMのミュート状態を設定する
    /// trueでOFF、falseでON
    /// </summary>
    public void SetBgmMute(bool isMuted)
    {
        isBgmMuted = isMuted;

        ApplyBgmVolume();
        SaveSoundSettings();
    }

    /// <summary>
    /// SEのミュート状態を設定する
    /// trueでOFF、falseでON
    /// </summary>
    public void SetSeMute(bool isMuted)
    {
        isSeMuted = isMuted;

        ApplySeVolume();
        SaveSoundSettings();
    }

    /// <summary>
    /// BGMのON/OFFを反転する
    /// </summary>
    public void ToggleBgmMute()
    {
        SetBgmMute(!isBgmMuted);
    }

    /// <summary>
    /// SEのON/OFFを反転する
    /// </summary>
    public void ToggleSeMute()
    {
        SetSeMute(!isSeMuted);
    }

    /// <summary>
    /// BGM音量をAudioSourceへ反映する
    /// </summary>
    private void ApplyBgmVolume()
    {
        if (bgmSource == null)
        {
            return;
        }

        float normalVolume = GetAppliedBgmVolume();

        /*
         * ポーズ中に設定を変更した場合は、
         * 新しい通常音量を保存する。
         */
        if (isBgmPaused)
        {
            bgmVolumeBeforePause = normalVolume;
        }

        float targetVolume = isBgmPaused
            ? normalVolume * pauseBGMVolumeRate
            : normalVolume;

        bgmFadeTween?.Kill();
        bgmSource.DOKill();

        bgmSource.volume = targetVolume;
    }

    /// <summary>
    /// SE音量をAudioSourceへ反映する
    /// </summary>
    private void ApplySeVolume()
    {
        if (seSource == null)
        {
            return;
        }

        seSource.volume = isSeMuted ? 0f : seVolume;
    }

    private float GetAppliedBgmVolume()
    {
        return isBgmMuted ? 0f : bgmVolume;
    }

    private void ApplyAllSoundSettings()
    {
        ApplyBgmVolume();
        ApplySeVolume();
    }

    // ==================================================
    // 設定保存
    // ==================================================

    /// <summary>
    /// 保存された音量・ミュート設定を読み込む
    /// </summary>
    private void LoadSoundSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat(
            SaveKeys.BgmVolume,
            defaultBgmVolume
        );

        seVolume = PlayerPrefs.GetFloat(
            SaveKeys.SeVolume,
            defaultSeVolume
        );

        isBgmMuted = PlayerPrefs.GetInt(
            SaveKeys.BgmMute,
            0
        ) == 1;

        isSeMuted = PlayerPrefs.GetInt(
            SaveKeys.SeMute,
            0
        ) == 1;

        bgmVolume = Mathf.Clamp01(bgmVolume);
        seVolume = Mathf.Clamp01(seVolume);
    }

    /// <summary>
    /// 現在の音量・ミュート設定を保存する
    /// </summary>
    private void SaveSoundSettings()
    {
        PlayerPrefs.SetFloat(
            SaveKeys.BgmVolume,
            bgmVolume
        );

        PlayerPrefs.SetFloat(
            SaveKeys.SeVolume,
            seVolume
        );

        PlayerPrefs.SetInt(
            SaveKeys.BgmMute,
            isBgmMuted ? 1 : 0
        );

        PlayerPrefs.SetInt(
            SaveKeys.SeMute,
            isSeMuted ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    /// <summary>
    /// 音設定を初期状態へ戻す
    /// </summary>
    public void ResetSoundSettings()
    {
        bgmVolume = defaultBgmVolume;
        seVolume = defaultSeVolume;

        isBgmMuted = false;
        isSeMuted = false;

        ApplyAllSoundSettings();
        SaveSoundSettings();
    }

    /// <summary>
    /// ポーズ時にBGMを小さくする。
    /// ポーズ前の実際のAudioSource音量を保存する。
    /// </summary>
    public void FadeDownBGMForPause()
    {
        if (bgmSource == null)
        {
            return;
        }

        // すでにポーズ用フェードを実行済みなら重ねて保存しない
        if (isBgmPaused)
        {
            return;
        }

        isBgmPaused = true;

        // 現在実際に鳴っている音量を保存
        bgmVolumeBeforePause = bgmSource.volume;

        bgmSource.DOKill();

        float targetVolume =
            bgmVolumeBeforePause * pauseBGMVolumeRate;

        bgmSource
            .DOFade(
                targetVolume,
                pauseBGMFadeDuration
            )
            .SetUpdate(true);
    }

    /// <summary>
    /// ポーズ解除時に、ポーズ直前のBGM音量へ戻す。
    /// </summary>
    public void RestoreBGMFromPause()
    {
        if (bgmSource == null)
        {
            return;
        }

        if (!isBgmPaused)
        {
            return;
        }

        isBgmPaused = false;

        bgmSource.DOKill();

        /*
         * SettingsPopupで変更された最新の音量と、
         * 最新のON/OFF状態を使って復帰する。
         */
        float targetVolume = GetAppliedBgmVolume();

        bgmSource
            .DOFade(
                targetVolume,
                pauseBGMFadeDuration
            )
            .SetUpdate(true);
    }
}
/*
SE
if (SoundManager.Instance != null)
{
    SoundManager.Instance.PlaySE(SEType.Correct);
}

Bgm
if (SoundManager.Instance != null)
{
    SoundManager.Instance.PlayBGM(BGMType.Game);
}

2秒かけてりざるとBGMへ変える
if (SoundManager.Instance != null)
{
    SoundManager.Instance.PlayBGM(BGMType.Result, 2f);
}

BGMをフェードアウト
if (SoundManager.Instance != null)
{
    SoundManager.Instance.StopBGM(1.5f);
}

Sliderとの接続
SoundManager.Instance.SetBgmVolume(value);
SoundManager.Instance.SetSeVolume(value);
*/