using UnityEngine;

/// <summary>
/// PopupManagerで管理するPopupの種類。
/// </summary>
public enum PopupType
{
    Pause,
    Result,
    Home,
    Settings,
    OpenSourceLicenses,
    LicenseDetail,

    // ステージ途中データがある場合の
    // 「続きから／最初から」選択
    ContinueStage
}