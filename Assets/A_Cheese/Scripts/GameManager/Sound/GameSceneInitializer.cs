using UnityEngine;

/// <summary>
/// ゲームシーン開始時の初期化処理を担当する。
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    private void Start()
    {
        // InitializeBGM();
        InitializeRRandamBGM();
    }

    /// <summary>
    /// ゲーム用BGMへ切り替える。
    /// </summary>
    private void InitializeBGM()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning(
                "GameSceneInitializer：SoundManagerが見つかりません。",
                this
            );

            return;
        }

        SoundManager.Instance.PlayBGM(BGMType.Game);
    }

    private void InitializeRRandamBGM()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning(
                "GameSceneInitializer：SoundManagerが見つかりません。",
                this
            );

            return;
        }

        // Gameに登録したBGMをランダムループ
        SoundManager.Instance.PlayRandomBGM(
            BGMType.Game
        );
    }
}

/*
 
// Game：複数曲をランダムループ
SoundManager.Instance.PlayRandomBGM(
    BGMType.Game
);

// Result：先頭の1曲を固定ループ
SoundManager.Instance.PlayBGM(
    BGMType.Result
);

// Title：先頭の1曲を固定ループ
SoundManager.Instance.PlayBGM(
    BGMType.Title
);

*/