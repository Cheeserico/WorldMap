using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("タイマー設定")]
    [SerializeField] private bool startAutomatically = true;

    // ゲーム開始からの経過時間
    private float elapsedTime;

    // 現在タイマーが動いているか
    private bool isRunning;

    // 経過時間を他のスクリプトから取得するためのプロパティ
    public float ElapsedTime => elapsedTime;

    // タイマーが動いているかを取得するためのプロパティ
    public bool IsRunning => isRunning;

    private void Start()
    {
        ResetTimer();

        if (startAutomatically)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        // Time.timeScaleの影響を受ける時間
        elapsedTime += Time.deltaTime;

        UpdateTimerText();
    }

    /// <summary>
    /// タイマーを開始・再開する
    /// </summary>
    public void StartTimer()
    {
        isRunning = true;
    }

    /// <summary>
    /// タイマーを停止する
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// タイマーを0に戻す
    /// </summary>
    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = false;

        UpdateTimerText();
    }

    /// <summary>
    /// 保存されていた経過時間を設定する。
    /// 設定しただけではタイマーは開始しない。
    /// </summary>
    public void SetElapsedTime(float time)
    {
        elapsedTime =
            Mathf.Max(0f, time);

        UpdateTimerText();

        Debug.Log(
            $"TimerManager：経過時間を" +
            $"{elapsedTime:F1}秒に設定しました。"
        );
    }

    /// <summary>
    /// タイマーを0に戻して開始する
    /// </summary>
    public void RestartTimer()
    {
        ResetTimer();
        StartTimer();
    }

    /// <summary>
    /// TimerTextの表示を更新する
    /// </summary>
    private void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes}:{seconds:00}";
    }
}

// コマンド一覧
// タイマー停止
// timerManager.StopTimer();
// タイマー再開
// timerManager.StartTimer();
// 全問正解した時
// timerManager.StopTimer();
// もう一度最初から
// timerManager.RestartTimer();
// クリアタイムの時間を出す
// float clearTime = timerManager.ElapsedTime;