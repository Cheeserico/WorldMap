using System;
using System.Collections.Generic;

/// <summary>
/// ステージの途中保存内容。
/// JsonUtilityでJSONへ変換し、PlayerPrefsへ保存する。
/// </summary>
[Serializable]
public class ContinueSaveData
{
    // 途中データが存在するか
    public bool hasSaveData;

    // プレイ中のステージID
    public string stageId;

    // 配置済みの国ID一覧
    public List<string> placedCountryIds =
        new List<string>();

    // 経過時間
    public float elapsedTime;

    // MapContentの位置
    public float mapPositionX;
    public float mapPositionY;

    // MapContentの拡大率
    public float mapScale = 1f;
}