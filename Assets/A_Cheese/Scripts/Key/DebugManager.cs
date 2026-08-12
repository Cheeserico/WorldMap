using UnityEngine;

/// <summary>
/// 開発中のセーブデータ確認・リセット用。
/// </summary>
public class DebugManager : MonoBehaviour
{
    private void Start()
    {
        //  ResetAllPlayerPrefs();
    }

    // ==================================================
    // ベストタイム
    // ==================================================

    [ContextMenu("Save / Reset Best Time")]
    [ContextMenu("Save / Reset Best Time")]
    public void ResetBestTime()
    {
        CountryPuzzleManager puzzleManager =
            FindFirstObjectByType<CountryPuzzleManager>();

        if (puzzleManager == null)
        {
            Debug.LogError(
                "CountryPuzzleManager が見つかりません。"
            );

            return;
        }

        string stageId =
            puzzleManager.CurrentStageId;

        if (string.IsNullOrEmpty(stageId))
        {
            Debug.LogError(
                "StageId が設定されていません。"
            );

            return;
        }

        string bestTimeKey =
            SaveKeys.GetBestTimeKey(stageId);

        PlayerPrefs.DeleteKey(bestTimeKey);
        PlayerPrefs.Save();

        Debug.Log(
            $"DebugManager：{bestTimeKey} を削除しました。"
        );
    }


    // ==================================================
    // Sound
    // ==================================================

    [ContextMenu("Save / Reset Sound Settings")]
    public void ResetSoundSettings()
    {
        PlayerPrefs.DeleteKey(
            SaveKeys.BgmVolume
        );

        PlayerPrefs.DeleteKey(
            SaveKeys.SeVolume
        );

        PlayerPrefs.DeleteKey(
            SaveKeys.BgmMute
        );

        PlayerPrefs.DeleteKey(
            SaveKeys.SeMute
        );

        PlayerPrefs.Save();

        Debug.Log(
            "DebugManager：Sound設定を削除しました。"
        );
    }


    // ==================================================
    // 全PlayerPrefs削除
    // ==================================================

    [ContextMenu("Save / RESET ALL PlayerPrefs")]
    public void ResetAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.LogWarning(
            "DebugManager：PlayerPrefsをすべて削除しました。"
        );
    }


    // ==================================================
    // 保存内容確認
    // ==================================================

    [ContextMenu("Save / Print Save Data")]
    public void PrintSaveData()
    {
        Debug.Log(
            "========== SAVE DATA =========="
        );

        PrintFloat(
            SaveKeys.BgmVolume
        );

        PrintFloat(
            SaveKeys.SeVolume
        );

        PrintInt(
            SaveKeys.BgmMute
        );

        PrintInt(
            SaveKeys.SeMute
        );

        CountryPuzzleManager puzzleManager =
            FindFirstObjectByType<CountryPuzzleManager>();

        if (puzzleManager != null)
        {
            string stageId =
                puzzleManager.CurrentStageId;

            if (!string.IsNullOrEmpty(stageId))
            {
                PrintFloat(
                    SaveKeys.GetBestTimeKey(stageId)
                );
            }
        }

        Debug.Log(
            "==============================="
        );
    }


    private void PrintFloat(
        string key)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            Debug.Log(
                $"{key} = 未保存"
            );

            return;
        }

        Debug.Log(
            $"{key} = " +
            PlayerPrefs.GetFloat(key)
        );
    }


    private void PrintInt(
        string key)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            Debug.Log(
                $"{key} = 未保存"
            );

            return;
        }

        Debug.Log(
            $"{key} = " +
            PlayerPrefs.GetInt(key)
        );
    }
}