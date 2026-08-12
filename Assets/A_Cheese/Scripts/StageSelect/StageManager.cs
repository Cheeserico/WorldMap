using UnityEngine;

/// <summary>
/// 選択されたStageDataを保持し、
/// SceneをまたいでGameSceneへ渡すためのManager。
/// </summary>
public class StageManager : MonoBehaviour
{
    // ==================================================
    // Singleton
    // ==================================================

    public static StageManager Instance { get; private set; }


    // ==================================================
    // 選択中のステージ
    // ==================================================

    public StageData SelectedStageData { get; private set; }


    // ==================================================
    // 初期化
    // ==================================================

    private void Awake()
    {
        // すでにStageManagerが存在している場合、
        // 重複した自分を削除する
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Sceneが変わってもStageManagerを残す
        DontDestroyOnLoad(gameObject);
    }


    // ==================================================
    // StageData設定
    // ==================================================

    public void SetStageData(StageData stageData)
    {
        SelectedStageData = stageData;

        Debug.Log(
            $"StageManager：{stageData.name} を選択しました。"
        );
    }
}