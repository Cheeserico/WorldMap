using UnityEngine;

/// <summary>
/// GameSceneをどの状態で開始するか。
/// </summary>
public enum StageStartMode
{
    NewGame,
    Continue
}

/// <summary>
/// 選択されたStageDataと開始モードを保持し、
/// SceneをまたいでGameSceneへ渡すためのManager。
/// </summary>
public class StageManager : MonoBehaviour
{
    // ==================================================
    // Singleton
    // ==================================================

    public static StageManager Instance
    {
        get;
        private set;
    }


    // ==================================================
    // 選択中のステージ
    // ==================================================

    public StageData SelectedStageData
    {
        get;
        private set;
    }


    // ==================================================
    // 開始モード
    // ==================================================

    public StageStartMode StartMode
    {
        get;
        private set;
    } = StageStartMode.NewGame;


    // ==================================================
    // 初期化
    // ==================================================

    private void Awake()
    {
        // すでにStageManagerが存在している場合、
        // 重複した自分を削除する
        if (Instance != null &&
            Instance != this)
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

    public void SetStageData(
        StageData stageData
    )
    {
        if (stageData == null)
        {
            Debug.LogWarning(
                "StageManager：" +
                "設定するStageDataがありません。"
            );

            return;
        }

        SelectedStageData =
            stageData;

        Debug.Log(
            $"StageManager：" +
            $"{stageData.name}を選択しました。"
        );
    }


    // ==================================================
    // 開始モード設定
    // ==================================================

    public void SetStartMode(
        StageStartMode startMode
    )
    {
        StartMode =
            startMode;

        Debug.Log(
            $"StageManager：" +
            $"開始モードを{StartMode}に設定しました。"
        );
    }
}