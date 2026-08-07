using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 設定PopupのUIを管理する。
///
/// ・BGM音量
/// ・SE音量
/// ・BGM ON/OFF
/// ・SE ON/OFF
/// ・SettingsPopupの開閉
///
/// を担当する。
/// 保存処理はSoundManager側で行う。
/// </summary>
public class SettingsPopup : MonoBehaviour
{
    [Header("BGM設定")]
    [SerializeField]
    private Slider bgmVolumeSlider;

    [SerializeField]
    private Toggle bgmOnOffToggle;

    [Header("SE設定")]
    [SerializeField]
    private Slider seVolumeSlider;

    [SerializeField]
    private Toggle seOnOffToggle;

    /// <summary>
    /// PausePopupのSettingsボタンから呼び出す。
    /// 現在の音設定をUIへ反映してからPopupを開く。
    /// </summary>
    public void OpenSettings()
    {
        if (PopupManager.Instance == null)
        {
            Debug.LogError(
                "SettingsPopup：PopupManagerが見つかりません。",
                this
            );

            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogError(
                "SettingsPopup：SoundManagerが見つかりません。",
                this
            );

            return;
        }

        // SettingsPopupを開く
        PopupManager.Instance.Open(PopupType.Settings);

        // SoundManagerの保存済み設定をUIへ反映
        RefreshUI();
    }
    /// <summary>
    /// SoundManagerの現在値をUIへ反映する。
    /// </summary>
    public void RefreshUI()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogError(
                "SettingsPopup：SoundManagerが見つかりません。",
                this
            );

            return;
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.SetValueWithoutNotify(
                SoundManager.Instance.BgmVolume
            );
        }

        if (seVolumeSlider != null)
        {
            seVolumeSlider.SetValueWithoutNotify(
                SoundManager.Instance.SeVolume
            );
        }

        if (bgmOnOffToggle != null)
        {
            bgmOnOffToggle.SetIsOnWithoutNotify(
                !SoundManager.Instance.IsBgmMuted
            );
        }

        if (seOnOffToggle != null)
        {
            seOnOffToggle.SetIsOnWithoutNotify(
                !SoundManager.Instance.IsSeMuted
            );
        }
    }
    /// <summary>
    /// BGM音量スライダーが動いたとき。
    /// </summary>
    public void OnBGMVolumeChanged(float volume)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.SetBgmVolume(volume);
    }

    /// <summary>
    /// SE音量スライダーが動いたとき。
    /// </summary>
    public void OnSEVolumeChanged(float volume)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.SetSeVolume(volume);
    }

    /// <summary>
    /// BGMのON/OFFが変更されたとき。
    ///
    /// isOnがtrueなら音を出すため、ミュートはfalse。
    /// isOnがfalseなら音を止めるため、ミュートはtrue。
    /// </summary>
    public void OnBGMToggleChanged(bool isOn)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.SetBgmMute(!isOn);
    }

    /// <summary>
    /// SEのON/OFFが変更されたとき。
    /// </summary>
    public void OnSEToggleChanged(bool isOn)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.SetSeMute(!isOn);
    }

    /// <summary>
    /// 閉じるボタンが押されたとき。
    ///
    /// SettingsPopupだけを閉じる。
    /// 後ろのPausePopupは開いたままなので、
    /// 自動的にPausePopupへ戻る。
    /// </summary>
    public void OnCloseButtonClicked()
    {
        if (PopupManager.Instance == null)
        {
            return;
        }

        PopupManager.Instance.Close(PopupType.Settings);
    }

}