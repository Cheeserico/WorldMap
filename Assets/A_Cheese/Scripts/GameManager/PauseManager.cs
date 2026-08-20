using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームの一時停止状態を管理する。
///
/// Popupの表示処理はPopupManager、
/// BGMの音量処理はSoundManagerへ任せる。
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("シーン設定")]

    [Tooltip("タイトル画面のシーン名")]
    [SerializeField]
    private string titleSceneName = "TitleScene";

    [Header("ポーズ設定")]

    [Tooltip("ポーズ時に復元するため、停止前のTimeScaleを保存する")]
    [SerializeField]
    private float defaultTimeScale = 1f;

    // 現在ポーズ中か
    private bool isPaused;

    // シーン移動などの処理中か
    private bool isSceneTransitioning;

    // ポーズ前のTime.timeScale
    private float timeScaleBeforePause = 1f;

    /// <summary>
    /// 現在ポーズ中か。
    /// </summary>
    public bool IsPaused => isPaused;

    /// <summary>
    /// PauseButtonから呼び出す。
    /// </summary>
    public void PauseGame()
    {
        if (isPaused || isSceneTransitioning)
        {
            return;
        }

        if (PopupManager.Instance == null)
        {
            Debug.LogError(
                "PauseManager：PopupManagerが存在しません。",
                this
            );

            return;
        }

        HintManager hintManager =
    FindFirstObjectByType<HintManager>();

        if (hintManager != null)
        {
            hintManager.StopAllHints();
        }

        /*
         * ポーズ前のTimeScaleを保存する。
         * 通常は1だが、スロー演出などにも対応できる。
         */
        timeScaleBeforePause = Time.timeScale;

        if (timeScaleBeforePause <= 0f)
        {
            timeScaleBeforePause = defaultTimeScale;
        }

        isPaused = true;

        /*
         * PopupBaseのDOTweenはSetUpdate(true)なので、
         * TimeScaleを0にしても開くアニメーションは動く。
         */
        Time.timeScale = 0f;

        PopupManager.Instance.Open(
            PopupType.Pause
        );

        FadeDownBGM();

        Debug.Log("ゲームを一時停止しました。");
    }

    /// <summary>
    /// ResumeButtonから呼び出す。
    ///
    /// PausePopupの閉じるアニメーションが終わってから
    /// ゲームを再開する。
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused || isSceneTransitioning)
        {
            return;
        }

        if (PopupManager.Instance == null)
        {
            CompleteResume();
            return;
        }

        PopupBase pausePopup =
            PopupManager.Instance.GetPopup<PopupBase>(
                PopupType.Pause
            );

        if (pausePopup == null)
        {
            CompleteResume();
            return;
        }

        /*
         * 閉じるアニメーション完了時に
         * CompleteResumeを一度だけ呼ぶ。
         */
        pausePopup.Closed -= HandlePausePopupClosed;
        pausePopup.Closed += HandlePausePopupClosed;

        PopupManager.Instance.Close(
            PopupType.Pause
        );
    }

    /// <summary>
    /// PausePopupが完全に閉じたときに呼ばれる。
    /// </summary>
    private void HandlePausePopupClosed()
    {
        if (PopupManager.Instance != null)
        {
            PopupBase pausePopup =
                PopupManager.Instance.GetPopup<PopupBase>(
                    PopupType.Pause
                );

            if (pausePopup != null)
            {
                pausePopup.Closed -=
                    HandlePausePopupClosed;
            }
        }

        CompleteResume();
    }

    /// <summary>
    /// ゲームを実際に再開する。
    /// </summary>
    private void CompleteResume()
    {
        Time.timeScale = timeScaleBeforePause;

        if (Time.timeScale <= 0f)
        {
            Time.timeScale = defaultTimeScale;
        }

        isPaused = false;

        RestoreBGM();

        Debug.Log("ゲームを再開しました。");
    }

    /// <summary>
    /// RestartButtonから呼び出す。
    /// 現在のゲームシーンを読み直す。
    /// </summary>
    public void RestartGame()
    {
        if (isSceneTransitioning)
        {
            return;
        }

        isSceneTransitioning = true;

        // 現在のステージの途中データを削除
        CountryPuzzleManager puzzleManager =
            FindFirstObjectByType<CountryPuzzleManager>();

        if (puzzleManager != null)
        {
            ContinueSaveManager.Delete(
                puzzleManager.CurrentStageId
            );
        }

        // 必ず最初から開始する
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SetStartMode(
                StageStartMode.NewGame
            );
        }

        PrepareForSceneChange();

        Scene currentScene =
            SceneManager.GetActiveScene();
        SceneManager.LoadScene(
            currentScene.name
        );
    }

    /// <summary>
    /// PausePopupの設定ボタンから呼び出す。
    ///
    /// PausePopupは閉じず、その上へSettingsPopupを表示する。
    /// </summary>
    public void OpenSettings()
    {
        if (!isPaused || isSceneTransitioning)
        {
            return;
        }

        if (PopupManager.Instance == null)
        {
            return;
        }

        PopupManager.Instance.Open(
            PopupType.Settings
        );
    }

    /// <summary>
    /// SettingsPopupの閉じるボタンから呼び出す。
    /// </summary>
    public void CloseSettings()
    {
        if (PopupManager.Instance == null)
        {
            return;
        }

        PopupManager.Instance.Close(
            PopupType.Settings
        );
    }

    /// <summary>
    /// タイトル画面へ戻る。
    /// </summary>
    public void ReturnToTitle()
    {
        if (isSceneTransitioning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(titleSceneName))
        {
            Debug.LogError(
                "PauseManager：タイトルシーン名が設定されていません。",
                this
            );

            return;
        }

        isSceneTransitioning = true;

        // タイトルへ移動する直前の状態を途中保存
        ContinueSaveController continueSaveController =
            FindFirstObjectByType<ContinueSaveController>();

        if (continueSaveController != null)
        {
            continueSaveController.SaveCurrentStage();
        }
        else
        {
            Debug.LogWarning(
                "PauseManager：" +
                "ContinueSaveControllerが見つからないため、" +
                "途中保存できません。",
                this
            );
        }

        PrepareForSceneChange();

        SceneManager.LoadScene(
            titleSceneName
        );
    }

    /// <summary>
    /// シーン移動前の共通処理。
    /// </summary>
    private void PrepareForSceneChange()
    {
        /*
         * TimeScaleが0のまま次のシーンへ移動すると、
         * タイトル画面まで停止してしまうため必ず戻す。
         */
        Time.timeScale = defaultTimeScale;

        isPaused = false;

        RestoreBGM();
    }

    /// <summary>
    /// ポーズ時にBGMをフェードダウンする。
    /// </summary>
    private void FadeDownBGM()
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.FadeDownBGMForPause();
    }

    /// <summary>
    /// 再開時にBGM音量を戻す。
    /// </summary>
    private void RestoreBGM()
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.RestoreBGMFromPause();
    }

    private void OnDestroy()
    {
        /*
         * ゲーム終了やシーン移動時に、
         * TimeScaleが0のまま残ることを防ぐ。
         */
        if (isPaused)
        {
            Time.timeScale = defaultTimeScale;
        }
    }
}