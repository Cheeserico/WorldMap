using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    public static ResultManager Instance { get; private set; }


    [Header("タイマー")]
    [SerializeField]
    private TimerManager timerManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// ゲームクリア時に呼ぶ
    /// </summary>
    public void ShowResult()
    {
        float clearTime = 0f;

        // ------------------------------
        // タイマー停止・クリアタイム取得
        // ------------------------------
        if (timerManager != null)
        {
            timerManager.StopTimer();
            clearTime = timerManager.ElapsedTime;

            Debug.Log($"クリアタイム：{clearTime}");
        }
        else
        {
            Debug.LogWarning(
                "ResultManager：TimerManagerが設定されていません。",
                this
            );
        }

        // ------------------------------
        // ベストタイム保存・新記録判定
        // ------------------------------
        bool isNewRecord =
            SaveBestTime(clearTime);

        string bestTimeKey = GetCurrentBestTimeKey();

        float bestTime =
            PlayerPrefs.GetFloat(
                bestTimeKey,
                clearTime
            );


        // ------------------------------
        // ResultPopupを取得
        // ------------------------------
        ResultPopup resultPopup = null;

        if (PopupManager.Instance != null)
        {
            resultPopup =
                PopupManager.Instance.GetPopup<ResultPopup>(
                    PopupType.Result
                );
        }

        // ------------------------------
        // リザルト内容を設定
        // ------------------------------
        if (resultPopup != null)
        {
            resultPopup.SetResult(
                clearTime,
                bestTime,
                isNewRecord
            );
        }

        // ------------------------------
        // ResultPopupを表示
        // ------------------------------
        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.Open(
                PopupType.Result
            );
        }

        // ------------------------------
        // クリアSE
        // ------------------------------
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(
                SEType.Clear
            );

            // ResultBGMへ切り替え
            SoundManager.Instance.PlayBGM(
                BGMType.Result
            );
        }
    }

    /// <summary>
    /// ベストタイムを保存する。
    /// 新記録ならtrueを返す。
    /// </summary>
    private bool SaveBestTime(float clearTime)
    {
        string bestTimeKey = GetCurrentBestTimeKey();

        // 初回プレイ
        if (!PlayerPrefs.HasKey(bestTimeKey))
        {
            PlayerPrefs.SetFloat(
                bestTimeKey,
                clearTime
            );

            PlayerPrefs.Save();

            Debug.Log(
                $"初ベストタイム保存：{clearTime}"
            );

            return true;
        }

        float previousBestTime =
            PlayerPrefs.GetFloat(
                bestTimeKey
            );

        // 今回の方が速い場合
        if (clearTime < previousBestTime)
        {
            PlayerPrefs.SetFloat(
                bestTimeKey,
                clearTime
            );

            PlayerPrefs.Save();

            Debug.Log(
                $"ベストタイム更新！ " +
                $"{previousBestTime} → {clearTime}"
            );

            return true;
        }

        Debug.Log(
            $"ベストタイム更新なし：今回 {clearTime} " +
            $"/ BEST {previousBestTime}"
        );

        return false;
    }

    private string GetCurrentBestTimeKey()
    {
        CountryPuzzleManager puzzleManager =
            FindFirstObjectByType<CountryPuzzleManager>();

        if (puzzleManager == null)
        {
            Debug.LogError(
                "CountryPuzzleManager が見つかりません。"
            );

            return SaveKeys.GetBestTimeKey("unknown");
        }

        string stageId =
            puzzleManager.CurrentStageId;

        if (string.IsNullOrEmpty(stageId))
        {
            Debug.LogError(
                "StageId が設定されていません。"
            );

            return SaveKeys.GetBestTimeKey("unknown");
        }

        return SaveKeys.GetBestTimeKey(stageId);
    }

    /// <summary>
    /// もう一度遊ぶ
    /// </summary>
    public void Retry()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    /// <summary>
    /// タイトルへ戻る
    /// </summary>
    public void GoToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}