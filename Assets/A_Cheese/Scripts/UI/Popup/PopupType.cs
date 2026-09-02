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
    AmiriLicenseDetail,
    NotoLicenseDetail,
    RTLLicenseDetail,

    // 途中データの再開確認
    ContinueStage,

    // 言語選択
    Language
}