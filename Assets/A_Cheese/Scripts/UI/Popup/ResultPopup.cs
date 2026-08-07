using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultPopup : PopupBase
{
    [Header("クリアタイム表示")]
    [SerializeField]
    private TextMeshProUGUI clearTimeText;

    /// <summary>
    /// クリアタイムを表示する
    /// </summary>
    public void SetClearTime(float clearTime)
    {
        int minutes = Mathf.FloorToInt(clearTime / 60);
        int seconds = Mathf.FloorToInt(clearTime % 60);
        int centiseconds = Mathf.FloorToInt((clearTime - Mathf.Floor(clearTime)) * 100);

        clearTimeText.text = $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }

    public void OnRetryButton()
    {
        Close();

        ResultManager.Instance.Retry();
    }

    public void OnTitleButton()
    {
        Close();

        ResultManager.Instance.GoToTitle();
    }
}