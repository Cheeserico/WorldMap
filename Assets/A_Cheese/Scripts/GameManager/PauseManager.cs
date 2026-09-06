using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ポーズ・途中保存・シーン移動を担当。広告の呼び出しはHomeAdActionsに任せる。
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("シーン設定")]
    [Tooltip("タイトル画面のシーン名")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [Header("ポーズ設定")]
    [Tooltip("停止前のTimeScaleが無効な場合の復元値")]
    [SerializeField] private float defaultTimeScale = 1f;

    private bool isPaused;
    private bool isSceneTransitioning;
    private float timeScaleBeforePause = 1f;
    private bool isResuming;
    private PopupBase closingPausePopup;
    private Object homeRequestOwner;
    public bool IsPaused => isPaused;
    private bool Busy => isSceneTransitioning || homeRequestOwner != null || isResuming;

    public void PauseGame()
    {
        if (isPaused || Busy) return;
        if (PopupManager.Instance == null)
        {
            Debug.LogError("PauseManager：PopupManagerが存在しません。", this);
            return;
        }
        HintManager hintManager = FindFirstObjectByType<HintManager>();
        if (hintManager != null) hintManager.StopAllHints();
        timeScaleBeforePause = Time.timeScale;
        if (timeScaleBeforePause <= 0f) timeScaleBeforePause = defaultTimeScale;
        isPaused = true;
        Time.timeScale = 0f;
        PopupManager.Instance.Open(PopupType.Pause);
        FadeDownBGM();
        Debug.Log("ゲームを一時停止しました。");
    }

    public void ResumeGame()
    {
        if (!isPaused || Busy) return;
        if (PopupManager.Instance == null)
        {
            CompleteResume();
            return;
        }
        closingPausePopup = PopupManager.Instance.GetPopup<PopupBase>(PopupType.Pause);
        if (closingPausePopup == null)
        {
            CompleteResume();
            return;
        }
        isResuming = true;
        closingPausePopup.Closed -= HandlePausePopupClosed;
        closingPausePopup.Closed += HandlePausePopupClosed;
        PopupManager.Instance.Close(PopupType.Pause);
    }

    private void HandlePausePopupClosed()
    {
        UnsubscribePauseClosed();
        CompleteResume();
    }

    private void UnsubscribePauseClosed()
    {
        if (closingPausePopup != null)
            closingPausePopup.Closed -= HandlePausePopupClosed;
        closingPausePopup = null;
    }

    private void CompleteResume()
    {
        if (isSceneTransitioning || homeRequestOwner != null) return;
        isResuming = false;
        Time.timeScale = timeScaleBeforePause;
        if (Time.timeScale <= 0f) Time.timeScale = defaultTimeScale;
        isPaused = false;
        RestoreBGM();
        Debug.Log("ゲームを再開しました。");
    }

    public void RestartGame()
    {
        if (Busy) return;
        isSceneTransitioning = true;
        CountryPuzzleManager puzzleManager = FindFirstObjectByType<CountryPuzzleManager>();
        if (puzzleManager != null) ContinueSaveManager.Delete(puzzleManager.CurrentStageId);
        if (StageManager.Instance != null) StageManager.Instance.SetStartMode(StageStartMode.NewGame);
        PrepareForSceneChange();
        SceneTransitionManager.ReloadCurrentScene();
    }

    public void OpenSettings()
    {
        if (!isPaused || Busy || PopupManager.Instance == null) return;
        PopupManager.Instance.Open(PopupType.Settings);
    }

    public void CloseSettings()
    {
        if (Busy || PopupManager.Instance == null) return;
        PopupManager.Instance.Close(PopupType.Settings);
    }

    /// <summary>
    /// HomeAdActions専用の移動予約。広告の種類を知らずに二重操作を防ぐ。
    /// 広告中のアプリ終了に備え、表示前にも途中保存を行う。
    /// </summary>
    public bool TryReserveHomeTransition(Object owner)
    {
        if (!isActiveAndEnabled || owner == null || Busy || !ValidateTitle()) return false;
        homeRequestOwner = owner;
        try { SaveBeforeTitle(); }
        catch (System.Exception error)
        {
            homeRequestOwner = null;
            Debug.LogException(error, this);
            return false;
        }
        return true;
    }

    public void CancelHomeTransition(Object owner)
    {
        if (homeRequestOwner == owner) homeRequestOwner = null;
    }

    public void CompleteHomeTransition(Object owner)
    {
        if (owner == null || homeRequestOwner != owner) return;
        homeRequestOwner = null;
        ReturnToTitle();
    }

    /// <summary>広告なしでタイトルへ戻る既存API。広告用ボタンから直接登録しない。</summary>
    public void ReturnToTitle()
    {
        if (Busy || !ValidateTitle()) return;
        isSceneTransitioning = true;
        SaveBeforeTitle();
        PrepareForSceneChange();
        SceneTransitionManager.Load(
            titleSceneName
        );
    }

    private bool ValidateTitle()
    {
        if (!string.IsNullOrWhiteSpace(titleSceneName)) return true;
        Debug.LogError("PauseManager：タイトルシーン名が設定されていません。", this);
        return false;
    }

    private void SaveBeforeTitle()
    {
        ContinueSaveController controller = FindFirstObjectByType<ContinueSaveController>();
        if (controller != null) controller.SaveCurrentStage();
        else Debug.LogWarning("PauseManager：ContinueSaveControllerが見つからないため、途中保存できません。", this);
    }

    private void PrepareForSceneChange()
    {
        UnsubscribePauseClosed();
        isResuming = false;
        Time.timeScale = defaultTimeScale;
        isPaused = false;
        RestoreBGM();
    }

    private void FadeDownBGM()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.FadeDownBGMForPause();
    }

    private void RestoreBGM()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.RestoreBGMFromPause();
    }

    private void OnDestroy()
    {
        UnsubscribePauseClosed();
        if (isPaused) Time.timeScale = defaultTimeScale;
    }
}
