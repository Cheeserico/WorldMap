/// <summary>
/// セーブデータで使用するキーを一括管理する。
///
/// PlayerPrefsやEasy Save 3で使用するキーは、
/// 原則ここに追加する。
/// </summary>
public static class SaveKeys
{
    // ==================================================
    // Sound
    // ==================================================

    public const string BgmVolume =
        "Sound_BGMVolume";

    public const string SeVolume =
        "Sound_SEVolume";

    public const string BgmMute =
        "Sound_BGMMute";

    public const string SeMute =
        "Sound_SEMute";


    // ==================================================
    // Result
    // ==================================================

    public const string BestTime =
        "BestTime";
}