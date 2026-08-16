using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 正解・不正解時に表示するフィードバックUIを管理する。
/// </summary>
public class AnswerFeedbackUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private RectTransform feedbackRect;

    [SerializeField]
    private Image flagImage;

    [SerializeField]
    private TMP_Text countryNameText;

    [SerializeField]
    private TMP_Text resultText;

    [Header("国旗データ")]
    [SerializeField]
    private CountryFlagDatabase countryFlagDatabase;


    [Header("表示時間")]
    [SerializeField]
    private float displayDuration = 0.8f;

    [SerializeField]
    private float fadeDuration = 0.2f;


    [Header("正解演出")]
    [SerializeField]
    private float correctStartScale = 0.7f;

    [SerializeField]
    private float correctPunchScale = 0.15f;


    [Header("不正解演出")]
    [SerializeField]
    private float wrongShakeDuration = 0.35f;

    [SerializeField]
    private float wrongShakeStrength = 18f;


    private Sequence currentSequence;


    private void Awake()
    {
        HideImmediately();
    }


    /// <summary>
    /// 正解演出を表示する。
    /// </summary>
    public void ShowCorrect(
        string countryName,
        Sprite flagSprite)
    {
        PrepareFeedback(
            countryName,
            flagSprite,
            "Correct!"
        );

        feedbackRect.localScale =
            Vector3.one * correctStartScale;

        currentSequence = DOTween.Sequence();

        currentSequence.Append(
            feedbackRect
                .DOScale(1f, 0.18f)
                .SetEase(Ease.OutBack)
        );

        currentSequence.Append(
            feedbackRect.DOPunchScale(
                Vector3.one * correctPunchScale,
                0.2f,
                5,
                0.5f
            )
        );

        currentSequence.AppendInterval(
            displayDuration
        );

        currentSequence.Append(
            canvasGroup.DOFade(
                0f,
                fadeDuration
            )
        );

        currentSequence.OnComplete(
            HideImmediately
        );
    }


    /// <summary>
    /// 不正解演出を表示する。
    /// </summary>
    public void ShowWrong(
        string countryName,
        Sprite flagSprite)
    {
        PrepareFeedback(
            countryName,
            flagSprite,
            "Wrong!"
        );

        feedbackRect.localScale =
            Vector3.one;

        currentSequence = DOTween.Sequence();

        currentSequence.Append(
            feedbackRect.DOShakeAnchorPos(
                wrongShakeDuration,
                wrongShakeStrength,
                12,
                90f,
                false,
                true
            )
        );

        currentSequence.AppendInterval(
            0.3f
        );

        currentSequence.Append(
            canvasGroup.DOFade(
                0f,
                fadeDuration
            )
        );

        currentSequence.OnComplete(
            HideImmediately
        );
    }


    /// <summary>
    /// 表示前の共通設定。
    /// </summary>
    private void PrepareFeedback(
        string countryName,
        Sprite flagSprite,
        string result)
    {
        if (currentSequence != null)
        {
            currentSequence.Kill();
        }

        gameObject.SetActive(true);

        canvasGroup.alpha = 1f;

        feedbackRect.localScale =
            Vector3.one;

        feedbackRect.anchoredPosition =
            Vector2.zero;

        countryNameText.text =
            countryName;

        resultText.text =
            result;

        flagImage.sprite =
            flagSprite;

        flagImage.enabled =
            flagSprite != null;
    }


    /// <summary>
    /// 即座に非表示にする。
    /// </summary>
    private void HideImmediately()
    {
        if (currentSequence != null)
        {
            currentSequence.Kill();
            currentSequence = null;
        }

        canvasGroup.alpha = 0f;

        feedbackRect.localScale =
            Vector3.one;

        feedbackRect.anchoredPosition =
            Vector2.zero;

        // gameObject.SetActive(false);
    }

    // ==================================================
    // 開発テスト用
    // ==================================================

    [ContextMenu("Test / Correct JPN")]
    private void TestCorrect()
    {
        Sprite flag =
            countryFlagDatabase.GetFlag("JPN");

        ShowCorrect(
            "Japan",
            flag
        );
    }


    [ContextMenu("Test / Wrong JPN")]
    private void TestWrong()
    {
        Sprite flag =
            countryFlagDatabase.GetFlag("JPN");

        ShowWrong(
            "Japan",
            flag
        );
    }
}