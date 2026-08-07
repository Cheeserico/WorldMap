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

        // タイマー停止
        if (timerManager != null)
        {
            timerManager.StopTimer();

            clearTime = timerManager.ElapsedTime;

            Debug.Log($"クリアタイム：{clearTime}");
        }

        // ResultPopupを取得
        ResultPopup resultPopup = null;

        if (PopupManager.Instance != null)
        {
            resultPopup =
                PopupManager.Instance.GetPopup<ResultPopup>(
                    PopupType.Result
                );
        }

        // クリアタイムを表示
        if (resultPopup != null)
        {
            resultPopup.SetClearTime(clearTime);
        }

        // ResultPopupを開く
        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.Open(PopupType.Result);
        }

        // クリアSE
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(SEType.Clear);
        }
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