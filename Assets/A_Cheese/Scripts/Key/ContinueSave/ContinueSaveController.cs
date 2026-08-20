using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイ中の途中保存・復元と、
/// バックグラウンド移行時の自動保存を管理する。
/// </summary>
public class ContinueSaveController : MonoBehaviour
{
    [Header("ゲーム進行")]
    [SerializeField]
    private CountryPuzzleManager puzzleManager;

    [Header("タイマー")]
    [SerializeField]
    private TimerManager timerManager;

    [Header("地図操作")]
    [SerializeField]
    private MapPanZoomController mapPanZoomController;

    // 各スクリプトのStartが完了したか
    private bool initializationComplete;

    // バックグラウンドへ移ったことがあるか
    private bool applicationWasPaused;

    // バックグラウンド移行前に
    // タイマーが動いていたか
    private bool timerWasRunningBeforePause;


    /// <summary>
    /// 各オブジェクトのStart完了後に、
    /// 必要なら途中データを復元する。
    /// </summary>
    private IEnumerator Start()
    {
        // CountryPieceDragやTimerManager、
        // MapPanZoomControllerのStart完了を待つ
        yield return null;

        initializationComplete = true;

        if (StageManager.Instance == null)
        {
            yield break;
        }

        if (StageManager.Instance.StartMode !=
            StageStartMode.Continue)
        {
            yield break;
        }

        RestoreCurrentStage();
    }


    /// <summary>
    /// 現在のゲーム状態を途中保存する。
    /// </summary>
    /// 
    [ContextMenu("Test / Save Current Stage")]

    public void SaveCurrentStage()
    {
        if (!initializationComplete)
        {
            return;
        }

        if (puzzleManager == null ||
            timerManager == null ||
            mapPanZoomController == null)
        {
            Debug.LogWarning(
                "ContinueSaveController：" +
                "必要な参照が設定されていません。"
            );

            return;
        }

        // ランダム抽選ステージは途中保存対象外
        StageData currentStageData =
            puzzleManager.CurrentStageData;

        if (!ContinueSaveManager.SupportsContinueSave(
                currentStageData))
        {
            string randomStageId =
                puzzleManager.CurrentStageId;

            // 過去のテストなどで途中データが残っていたら削除
            if (ContinueSaveManager.HasSaveData(
                    randomStageId))
            {
                ContinueSaveManager.Delete(
                    randomStageId
                );
            }

            Debug.Log(
                $"途中保存対象外のステージです：" +
                $"{randomStageId}"
            );

            return;
        }

        // 全問正解後は途中データを保存しない
        if (puzzleManager.IsGameClearScheduled)
        {
            return;
        }

        string stageId =
            puzzleManager.CurrentStageId;

        if (string.IsNullOrEmpty(stageId))
        {
            Debug.LogWarning(
                "ContinueSaveController：" +
                "StageIdが空のため保存できません。"
            );

            return;
        }

        List<string> placedCountryIds =
            puzzleManager.GetPlacedCountryIds();

        ContinueSaveManager.Save(
            stageId,
            placedCountryIds,
            timerManager.ElapsedTime,
            mapPanZoomController.CurrentMapPosition,
            mapPanZoomController.CurrentMapScale
        );
    }


    /// <summary>
    /// 現在のステージの途中データを復元する。
    /// </summary>
    public void RestoreCurrentStage()
    {
        if (puzzleManager == null ||
            timerManager == null ||
            mapPanZoomController == null)
        {
            Debug.LogWarning(
                "ContinueSaveController：" +
                "必要な参照が設定されていません。"
            );

            return;
        }

        string stageId =
            puzzleManager.CurrentStageId;

        if (string.IsNullOrEmpty(stageId))
        {
            return;
        }

        bool loaded =
            ContinueSaveManager.TryLoad(
                stageId,
                out ContinueSaveData saveData
            );

        if (!loaded)
        {
            Debug.LogWarning(
                $"途中データが見つかりません。" +
                $"最初から開始します。StageId={stageId}"
            );

            if (StageManager.Instance != null)
            {
                StageManager.Instance.SetStartMode(
                    StageStartMode.NewGame
                );
            }

            return;
        }

        // 復元中に時間が増えないよう停止
        timerManager.StopTimer();

        // 配置済みピースと進捗を復元
        int restoredCount =
            puzzleManager.RestorePlacedCountries(
                saveData.placedCountryIds
            );

        // 地図位置と倍率を復元
        Vector2 savedMapPosition =
            new Vector2(
                saveData.mapPositionX,
                saveData.mapPositionY
            );

        mapPanZoomController.RestoreMapView(
            savedMapPosition,
            saveData.mapScale
        );

        // 経過時間を復元して再開
        timerManager.SetElapsedTime(
            saveData.elapsedTime
        );

        timerManager.StartTimer();

        // Continueは今回のScene開始時に使用済み。
        // Retryなどでは最初から開始できるよう戻す。
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SetStartMode(
                StageStartMode.NewGame
            );
        }

        Debug.Log(
            $"途中データからゲームを再開しました。 " +
            $"StageId={stageId}, " +
            $"復元数={restoredCount}, " +
            $"時間={saveData.elapsedTime:F1}秒"
        );
    }


    /// <summary>
    /// アプリがバックグラウンドへ移動・復帰したとき。
    /// </summary>
    private void OnApplicationPause(
        bool pauseStatus
    )
    {
        if (!initializationComplete)
        {
            return;
        }

        if (pauseStatus)
        {
            applicationWasPaused = true;

            if (timerManager != null)
            {
                timerWasRunningBeforePause =
                    timerManager.IsRunning;
            }

            // 停止する前の経過時間を保存
            SaveCurrentStage();

            if (timerManager != null)
            {
                timerManager.StopTimer();
            }

            Debug.Log(
                "バックグラウンド移行：" +
                "途中保存してタイマーを停止しました。"
            );
        }
        else
        {
            // アプリ起動直後のfalse通知では
            // タイマーを操作しない
            if (!applicationWasPaused)
            {
                return;
            }

            applicationWasPaused = false;

            if (timerManager != null &&
                timerWasRunningBeforePause &&
                !puzzleManager.IsGameClearScheduled)
            {
                timerManager.StartTimer();
            }

            timerWasRunningBeforePause = false;

            Debug.Log(
                "アプリ復帰：" +
                "タイマーを再開しました。"
            );
        }
    }


    /// <summary>
    /// アプリ終了時にも念のため保存する。
    /// </summary>
    private void OnApplicationQuit()
    {
        SaveCurrentStage();
    }
}