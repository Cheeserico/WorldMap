using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 途中データが存在するステージを選択したときに、
/// 「続きから／最初から」を選ぶPopup。
/// </summary>
public class ContinueStagePopup : PopupBase
{
    [Header("選択したステージ名")]
    [SerializeField]
    private TextMeshProUGUI stageNameText;

    // 選択されたStageData
    private StageData selectedStageData;

    // 移動先のGameScene名
    private string selectedGameSceneName;


    /// <summary>
    /// Popupを表示する前に、
    /// 選択したステージ情報を設定する。
    /// </summary>
    public void Setup(
        StageData stageData,
        string gameSceneName
    )
    {
        selectedStageData =
            stageData;

        selectedGameSceneName =
            gameSceneName;

        if (stageNameText != null &&
            selectedStageData != null)
        {
            stageNameText.text =
                selectedStageData.stageName;
        }
    }


    /// <summary>
    /// 「続きから」ボタン。
    /// </summary>
    public void ContinueGame()
    {
        if (!CanStartStage())
        {
            return;
        }

        StageManager.Instance.SetStageData(
            selectedStageData
        );

        StageManager.Instance.SetStartMode(
            StageStartMode.Continue
        );

        Debug.Log(
            $"続きから開始します：" +
            $"{selectedStageData.stageId}"
        );

        SceneManager.LoadScene(
            selectedGameSceneName
        );
    }


    /// <summary>
    /// 「最初から」ボタン。
    /// そのステージの途中データを削除して開始する。
    /// </summary>
    public void StartNewGame()
    {
        if (!CanStartStage())
        {
            return;
        }

        ContinueSaveManager.Delete(
            selectedStageData.stageId
        );

        StageManager.Instance.SetStageData(
            selectedStageData
        );

        StageManager.Instance.SetStartMode(
            StageStartMode.NewGame
        );

        Debug.Log(
            $"最初から開始します：" +
            $"{selectedStageData.stageId}"
        );

        SceneManager.LoadScene(
            selectedGameSceneName
        );
    }


    /// <summary>
    /// ステージ開始に必要な情報があるか確認する。
    /// </summary>
    private bool CanStartStage()
    {
        if (selectedStageData == null)
        {
            Debug.LogWarning(
                "ContinueStagePopup：" +
                "StageDataが設定されていません。"
            );

            return false;
        }

        if (StageManager.Instance == null)
        {
            Debug.LogError(
                "ContinueStagePopup：" +
                "StageManagerが見つかりません。"
            );

            return false;
        }

        if (string.IsNullOrEmpty(
                selectedGameSceneName))
        {
            Debug.LogWarning(
                "ContinueStagePopup：" +
                "GameScene名が設定されていません。"
            );

            return false;
        }

        return true;
    }
}