using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージごとの途中データを
/// PlayerPrefsへ保存・読込・削除する。
/// </summary>
public static class ContinueSaveManager
{
    /// <summary>
    /// このステージが途中保存に対応しているか。
    /// ランダム抽選ステージは対象外。
    /// </summary>
    public static bool SupportsContinueSave(
        StageData stageData
    )
    {
        if (stageData == null)
        {
            return false;
        }

        return !stageData.useRandomCountries;
    }

    /// <summary>
    /// 途中データを保存する。
    /// </summary>
    public static void Save(
        string stageId,
        List<string> placedCountryIds,
        float elapsedTime,
        Vector2 mapPosition,
        float mapScale
    )
    {
        if (string.IsNullOrEmpty(stageId))
        {
            Debug.LogError(
                "ContinueSaveManager：StageIdが空のため保存できません。"
            );

            return;
        }

        ContinueSaveData saveData =
            new ContinueSaveData
            {
                hasSaveData = true,
                stageId = stageId,

                placedCountryIds =
                    placedCountryIds != null
                        ? new List<string>(
                            placedCountryIds
                        )
                        : new List<string>(),

                elapsedTime =
                    Mathf.Max(0f, elapsedTime),

                mapPositionX =
                    mapPosition.x,

                mapPositionY =
                    mapPosition.y,

                mapScale =
                    Mathf.Max(0.01f, mapScale)
            };

        string json =
            JsonUtility.ToJson(saveData);

        string saveKey =
            SaveKeys.GetContinueSaveKey(
                stageId
            );

        PlayerPrefs.SetString(
            saveKey,
            json
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"途中データを保存しました。 " +
            $"StageId={stageId}, " +
            $"配置済み={saveData.placedCountryIds.Count}, " +
            $"経過時間={saveData.elapsedTime:F1}秒"
        );
    }


    /// <summary>
    /// 指定ステージに途中データが存在するか確認する。
    /// </summary>
    public static bool HasSaveData(
        string stageId
    )
    {
        if (string.IsNullOrEmpty(stageId))
        {
            return false;
        }

        string saveKey =
            SaveKeys.GetContinueSaveKey(
                stageId
            );

        return PlayerPrefs.HasKey(
            saveKey
        );
    }


    /// <summary>
    /// 途中データを読み込む。
    /// 読み込み成功時はtrueを返す。
    /// </summary>
    public static bool TryLoad(
        string stageId,
        out ContinueSaveData saveData
    )
    {
        saveData = null;

        if (!HasSaveData(stageId))
        {
            return false;
        }

        string saveKey =
            SaveKeys.GetContinueSaveKey(
                stageId
            );

        string json =
            PlayerPrefs.GetString(
                saveKey,
                ""
            );

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning(
                $"途中データが空です。StageId={stageId}"
            );

            return false;
        }

        saveData =
            JsonUtility.FromJson<ContinueSaveData>(
                json
            );

        if (saveData == null ||
            !saveData.hasSaveData ||
            saveData.stageId != stageId)
        {
            Debug.LogWarning(
                $"途中データが正しくありません。StageId={stageId}"
            );

            saveData = null;
            return false;
        }

        if (saveData.placedCountryIds == null)
        {
            saveData.placedCountryIds =
                new List<string>();
        }

        return true;
    }


    /// <summary>
    /// 指定ステージの途中データだけを削除する。
    /// </summary>
    public static void Delete(
        string stageId
    )
    {
        if (string.IsNullOrEmpty(stageId))
        {
            return;
        }

        string saveKey =
            SaveKeys.GetContinueSaveKey(
                stageId
            );

        PlayerPrefs.DeleteKey(
            saveKey
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"途中データを削除しました。StageId={stageId}"
        );
    }
}