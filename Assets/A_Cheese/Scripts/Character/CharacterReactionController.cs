using UnityEngine;

/// <summary>
/// キャラクターのリアクションアニメーションを管理する。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class CharacterReactionController : MonoBehaviour
{
    private static readonly int CorrectHash =
        Animator.StringToHash("Correct");

    [Header("キャラクターのAnimator")]
    [SerializeField]
    private Animator animator;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    /// <summary>
    /// 正解時の喜ぶジャンプを再生する。
    /// </summary>
    public void PlayCorrect()
    {
        if (animator == null)
        {
            Debug.LogWarning(
                "CharacterReactionController：" +
                "Animatorが設定されていません。"
            );

            return;
        }

        animator.ResetTrigger(CorrectHash);
        animator.SetTrigger(CorrectHash);
    }
}