using UnityEngine;

/// <summary>
/// ゲームシーン開始時の初期化処理を担当する。
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    private void Start()
    {
        InitializeBGM();
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
}