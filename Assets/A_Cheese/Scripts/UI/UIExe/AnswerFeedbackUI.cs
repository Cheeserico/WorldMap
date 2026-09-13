using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// 正解・不正解時に表示するフィードバックUIを管理する。
/// </summary>
public class AnswerFeedbackUI : MonoBehaviour
{
    [Header("全体")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Image darkBackground;


    [Header("正解UI")]
    [SerializeField]
    private GameObject correctEffectRoot;

    [SerializeField]
    private RectTransform correctTitleRect;

    [SerializeField]
    private RectTransform correctRingBackRect;

    [SerializeField]
    private RectTransform correctRingRect;

    [SerializeField]
    private RectTransform correctRotateLightRect;

    [SerializeField]
    private Image correctCountryImage;

    [SerializeField]
    private Image correctCharacterImage;

    [Tooltip("キャラクターから飛び出す3個の地域アイコン")]
    [SerializeField]
    private Image[] correctCharacterIcons =
    new Image[3];

    [SerializeField]
    private RectTransform correctCountryInfoPanel;

    [SerializeField]
    private Image correctFlagImage;

    [SerializeField]
    private TMP_Text correctCountryNameText;


    [Header("不正解UI")]
    [SerializeField]
    private GameObject wrongEffectRoot;

    [SerializeField]
    private RectTransform wrongTitleRect;

    [SerializeField]
    private Image wrongCountryImage;

    [SerializeField]
    private RectTransform wrongCountryInfoPanel;

    [SerializeField]
    private Image wrongFlagImage;

    [SerializeField]
    private TMP_Text wrongCountryNameText;


    [Header("表示時間")]
    [SerializeField]
    private float correctDisplayDuration = 0.8f;

    [SerializeField]
    private float wrongDisplayDuration = 0.55f;

    [SerializeField]
    private float fadeDuration = 0.2f;


    [Header("正解演出")]
    [SerializeField]
    private float correctCountryStartScale = 0.25f;

    [SerializeField]
    private float correctTitleStartOffsetY = 70f;

    [SerializeField]
    private float correctInfoStartOffsetY = -45f;

    [Header("地域アイコン演出")]

    [Tooltip("アイコンが飛び出す距離")]
    [SerializeField]
    private Vector2[] characterIconMoveOffsets =
{
    new Vector2(-100f, 100f),
    new Vector2(0f, 145f),
    new Vector2(100f, 100f)
};

    [SerializeField]
    private float characterIconStartScale = 0.2f;

    [SerializeField]
    private float characterIconMoveDuration = 0.45f;

    [SerializeField]
    private float characterIconInterval = 0.06f;


    [Header("不正解演出")]
    [SerializeField]
    private float wrongShakeDuration = 0.35f;

    [SerializeField]
    private float wrongShakeStrength = 18f;

    [SerializeField]
    private float wrongTitleStartOffsetY = 35f;

    [SerializeField]
    private float wrongInfoStartOffsetY = -35f;

    [Header("正解時の紙吹雪")]
    [SerializeField]
    private GameObject correctConfettiRoot;

    private ParticleSystem[] correctConfettiParticles;

    [Header("国旗データ")]
    [SerializeField]
    private CountryFlagDatabase countryFlagDatabase;


    [Header("キャラクターデータ")]
    [SerializeField]
    private CountryCharacterDatabase countryCharacterDatabase;

    private Sequence currentSequence;

    private Vector2 correctTitlePosition;
    private Vector2 correctInfoPosition;
    private Vector2 correctCountryPosition;

    private Vector2 wrongTitlePosition;
    private Vector2 wrongInfoPosition;
    private Vector2 wrongCountryPosition;

    private Vector2 correctCharacterPosition;

    private Vector2[] correctCharacterIconPositions;

    private void Awake()
    {
        SaveOriginalPositions();

        if (correctConfettiRoot != null)
        {
            correctConfettiParticles =
                correctConfettiRoot.GetComponentsInChildren<ParticleSystem>(
                    true
                );
        }

        HideImmediately();
    }

    /// <summary>
    /// UIの完成位置を保存する。
    /// </summary>
    private void SaveOriginalPositions()
    {
        if (correctTitleRect != null)
        {
            correctTitlePosition =
                correctTitleRect.anchoredPosition;
        }

        if (correctCountryInfoPanel != null)
        {
            correctInfoPosition =
                correctCountryInfoPanel.anchoredPosition;
        }

        if (correctCountryImage != null)
        {
            correctCountryPosition =
                correctCountryImage.rectTransform.anchoredPosition;
        }

        if (wrongTitleRect != null)
        {
            wrongTitlePosition =
                wrongTitleRect.anchoredPosition;
        }

        if (wrongCountryInfoPanel != null)
        {
            wrongInfoPosition =
                wrongCountryInfoPanel.anchoredPosition;
        }

        if (wrongCountryImage != null)
        {
            wrongCountryPosition =
                wrongCountryImage.rectTransform.anchoredPosition;
        }

        if (correctCharacterImage != null)
        {
            correctCharacterPosition =
                correctCharacterImage.rectTransform.anchoredPosition;
        }

        if (correctCharacterIcons != null)
        {
            correctCharacterIconPositions =
                new Vector2[correctCharacterIcons.Length];

            for (int i = 0;
                 i < correctCharacterIcons.Length;
                 i++)
            {
                Image icon =
                    correctCharacterIcons[i];

                if (icon == null)
                {
                    continue;
                }

                correctCharacterIconPositions[i] =
                    icon.rectTransform.anchoredPosition;
            }
        }
    }


    /// <summary>
    /// 正解演出を表示する。
    /// </summary>
    public void ShowCorrect(
        string countryId,
        Sprite flagSprite,
        Sprite countrySprite)
    {
        PrepareFeedback(
            true,
            countryId,
            flagSprite,
            countrySprite
        );

        PrepareCorrectAnimation();

        PlayCorrectConfetti();

        currentSequence =
            DOTween.Sequence();

        // 全体を素早く表示
        currentSequence.Insert(
            0f,
            canvasGroup.DOFade(
                1f,
                0.1f
            )
        );

        // 背景リング
        if (correctRingBackRect != null)
        {
            currentSequence.Insert(
                0f,
                correctRingBackRect
                    .DOScale(1f, 0.28f)
                    .SetEase(Ease.OutBack)
            );
        }

        // 装飾リング
        if (correctRingRect != null)
        {
            currentSequence.Insert(
                0.03f,
                correctRingRect
                    .DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
            );

            currentSequence.Insert(
                0.03f,
                correctRingRect
                    .DORotate(
                        Vector3.zero,
                        0.45f,
                        RotateMode.FastBeyond360
                    )
                    .SetEase(Ease.OutCubic)
            );
        }

        // 回転する光
        if (correctRotateLightRect != null)
        {
            currentSequence.Insert(
                0f,
                correctRotateLightRect
                    .DORotate(
                        new Vector3(0f, 0f, 180f),
                        0.75f,
                        RotateMode.FastBeyond360
                    )
                    .SetEase(Ease.OutCubic)
            );
        }

        // 国ピース
        if (correctCountryImage != null)
        {
            RectTransform countryRect =
                correctCountryImage.rectTransform;

            currentSequence.Insert(
                0.08f,
                countryRect
                    .DOScale(1f, 0.32f)
                    .SetEase(Ease.OutBack)
            );

            currentSequence.Insert(
                0.08f,
                countryRect
                    .DORotate(
                        Vector3.zero,
                        0.3f
                    )
                    .SetEase(Ease.OutCubic)
            );
        }

        // 地域キャラクター
        if (correctCharacterImage != null &&
            correctCharacterImage.enabled)
        {
            RectTransform characterRect =
                correctCharacterImage.rectTransform;

            currentSequence.Insert(
                0.12f,
                characterRect
                    .DOAnchorPos(
                        correctCharacterPosition,
                        0.3f
                    )
                    .SetEase(Ease.OutBack)
            );

            currentSequence.Insert(
                0.12f,
                characterRect
                    .DOScale(
                        1f,
                        0.3f
                    )
                    .SetEase(Ease.OutBack)
            );

            currentSequence.Insert(
                0.12f,
                characterRect
                    .DORotate(
                        Vector3.zero,
                        0.25f
                    )
                    .SetEase(Ease.OutCubic)
            );
        }

        // キャラクターから地域アイコンが飛び出す
        if (correctCharacterIcons != null)
        {
            for (int i = 0;
                 i < correctCharacterIcons.Length;
                 i++)
            {
                Image icon =
                    correctCharacterIcons[i];

                if (icon == null ||
                    !icon.enabled)
                {
                    continue;
                }

                RectTransform iconRect =
                    icon.rectTransform;

                Vector2 moveOffset =
                    Vector2.zero;

                if (characterIconMoveOffsets != null &&
                    i < characterIconMoveOffsets.Length)
                {
                    moveOffset =
                        characterIconMoveOffsets[i];
                }

                Vector2 startPosition =
                    iconRect.anchoredPosition;

                float startTime =
                    0.27f +
                    i * characterIconInterval;

                currentSequence.Insert(
                    startTime,
                    icon.DOFade(
                        1f,
                        0.08f
                    )
                );

                currentSequence.Insert(
                    startTime,
                    iconRect
                        .DOAnchorPos(
                            startPosition + moveOffset,
                            characterIconMoveDuration
                        )
                        .SetEase(Ease.OutBack)
                );

                currentSequence.Insert(
                    startTime,
                    iconRect
                        .DOScale(
                            1f,
                            0.22f
                        )
                        .SetEase(Ease.OutBack)
                );

                float endRotation =
                    i % 2 == 0
                        ? 12f
                        : -12f;

                currentSequence.Insert(
                    startTime,
                    iconRect
                        .DORotate(
                            new Vector3(
                                0f,
                                0f,
                                endRotation
                            ),
                            characterIconMoveDuration
                        )
                        .SetEase(Ease.OutCubic)
                );

                currentSequence.Insert(
                    startTime +
                    characterIconMoveDuration -
                    0.12f,
                    icon.DOFade(
                        0f,
                        0.18f
                    )
                );
            }
        }

        // NICE!
        if (correctTitleRect != null)
        {
            currentSequence.Insert(
                0.17f,
                correctTitleRect
                    .DOAnchorPos(
                        correctTitlePosition,
                        0.3f
                    )
                    .SetEase(Ease.OutBack)
            );

            currentSequence.Insert(
                0.17f,
                correctTitleRect
                    .DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
            );
        }

        // 国旗・国名
        if (correctCountryInfoPanel != null)
        {
            currentSequence.Insert(
                0.3f,
                correctCountryInfoPanel
                    .DOAnchorPos(
                        correctInfoPosition,
                        0.25f
                    )
                    .SetEase(Ease.OutBack)
            );

            currentSequence.Insert(
                0.3f,
                correctCountryInfoPanel
                    .DOScale(1f, 0.25f)
                    .SetEase(Ease.OutBack)
            );
        }

        currentSequence.AppendInterval(
            correctDisplayDuration
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
        string countryId,
        Sprite flagSprite,
        Sprite countrySprite)
    {
        PrepareFeedback(
            false,
            countryId,
            flagSprite,
            countrySprite
        );

        PrepareWrongAnimation();

        currentSequence =
            DOTween.Sequence();

        // 全体を素早く表示
        currentSequence.Insert(
            0f,
            canvasGroup.DOFade(
                1f,
                0.08f
            )
        );

        // TRY AGAIN!
        if (wrongTitleRect != null)
        {
            currentSequence.Insert(
                0f,
                wrongTitleRect
                    .DOAnchorPos(
                        wrongTitlePosition,
                        0.18f
                    )
                    .SetEase(Ease.OutCubic)
            );
        }

        // 国ピースを表示
        if (wrongCountryImage != null)
        {
            RectTransform countryRect =
                wrongCountryImage.rectTransform;

            currentSequence.Insert(
                0.04f,
                countryRect
                    .DOScale(1f, 0.15f)
                    .SetEase(Ease.OutCubic)
            );

            // 国ピースだけを左右に揺らす
            currentSequence.Insert(
                0.18f,
                countryRect.DOShakeAnchorPos(
                    wrongShakeDuration,
                    new Vector2(
                        wrongShakeStrength,
                        0f
                    ),
                    12,
                    90f,
                    false,
                    true
                )
            );
        }

        // 国旗・国名
        if (wrongCountryInfoPanel != null)
        {
            currentSequence.Insert(
                0.12f,
                wrongCountryInfoPanel
                    .DOAnchorPos(
                        wrongInfoPosition,
                        0.2f
                    )
                    .SetEase(Ease.OutCubic)
            );
        }

        currentSequence.AppendInterval(
            wrongDisplayDuration
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
    /// 共通の表示準備。
    /// </summary>
    private void PrepareFeedback(
        bool isCorrect,
        string countryId,
        Sprite flagSprite,
        Sprite countrySprite)
    {
        if (currentSequence != null)
        {
            currentSequence.Kill();
            currentSequence = null;
        }

        gameObject.SetActive(true);

        canvasGroup.alpha = 0f;

        if (correctEffectRoot != null)
        {
            correctEffectRoot.SetActive(
                isCorrect
            );
        }

        if (wrongEffectRoot != null)
        {
            wrongEffectRoot.SetActive(
                !isCorrect
            );
        }

        string localizedCountryName =
            GetLocalizedCountryName(
                countryId
            );

        if (isCorrect)
        {
            ApplyContent(
                correctCountryImage,
                correctFlagImage,
                correctCountryNameText,
                countrySprite,
                flagSprite,
                localizedCountryName
            );

            ApplyCorrectCharacter(
                countryId
            );

            ApplyCorrectCharacterIcons(
                countryId);
        }
        else
        {
            ApplyContent(
                wrongCountryImage,
                wrongFlagImage,
                wrongCountryNameText,
                countrySprite,
                flagSprite,
                localizedCountryName
            );
        }
    }


    /// <summary>
    /// 国画像・国旗・国名を反映する。
    /// </summary>
    private void ApplyContent(
        Image countryImage,
        Image flagImage,
        TMP_Text countryNameText,
        Sprite countrySprite,
        Sprite flagSprite,
        string localizedCountryName)
    {
        if (countryImage != null)
        {
            countryImage.sprite =
                countrySprite;

            countryImage.enabled =
                countrySprite != null;

            countryImage.preserveAspect =
                true;
        }

        if (flagImage != null)
        {
            flagImage.sprite =
                flagSprite;

            flagImage.enabled =
                flagSprite != null;

            flagImage.preserveAspect =
                true;
        }

        if (countryNameText != null)
        {
            countryNameText.text =
                localizedCountryName;
        }
    }

    /// <summary>
    /// 国IDに対応する正解キャラクターを設定する。
    /// 登録されていない国はDefault画像になる。
    /// </summary>
    private void ApplyCorrectCharacter(
        string countryId)
    {
        if (correctCharacterImage == null)
        {
            return;
        }

        Sprite characterSprite = null;

        if (countryCharacterDatabase != null)
        {
            characterSprite =
                countryCharacterDatabase.GetCharacterSprite(
                    countryId
                );
        }

        correctCharacterImage.sprite =
            characterSprite;

        correctCharacterImage.enabled =
            characterSprite != null;

        correctCharacterImage.preserveAspect =
            true;

        correctCharacterImage.raycastTarget =
            false;
    }

    /// <summary>
    /// 国IDに対応する地域アイコンを設定する。
    /// </summary>
    private void ApplyCorrectCharacterIcons(
        string countryId)
    {
        if (correctCharacterIcons == null)
        {
            return;
        }

        Sprite[] iconSprites = null;

        if (countryCharacterDatabase != null)
        {
            iconSprites =
                countryCharacterDatabase.GetEffectIcons(
                    countryId
                );
        }

        for (int i = 0;
             i < correctCharacterIcons.Length;
             i++)
        {
            Image icon =
                correctCharacterIcons[i];

            if (icon == null)
            {
                continue;
            }

            Sprite iconSprite = null;

            if (iconSprites != null &&
                i < iconSprites.Length)
            {
                iconSprite =
                    iconSprites[i];
            }

            icon.sprite =
                iconSprite;

            icon.enabled =
                iconSprite != null;

            icon.preserveAspect =
                true;

            icon.raycastTarget =
                false;

            Color iconColor =
                icon.color;

            iconColor.a = 0f;
            icon.color =
                iconColor;
        }
    }

    /// <summary>
    /// 国名をLocalization Tableから取得する。
    /// </summary>
    private string GetLocalizedCountryName(
        string countryId)
    {
        LocalizedString localizedCountryName =
            new LocalizedString(
                "CountryNames",
                countryId
            );

        return localizedCountryName
            .GetLocalizedString();
    }


    /// <summary>
    /// 正解時の紙吹雪を最初から再生する。
    /// </summary>
    private void PlayCorrectConfetti()
    {
        if (correctConfettiRoot == null)
        {
            return;
        }

        correctConfettiRoot.SetActive(true);

        if (correctConfettiParticles == null)
        {
            correctConfettiParticles =
                correctConfettiRoot.GetComponentsInChildren<ParticleSystem>(
                    true
                );
        }

        foreach (ParticleSystem particle
                 in correctConfettiParticles)
        {
            if (particle == null)
            {
                continue;
            }

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            particle.Play(
                true
            );
        }
    }

    /// <summary>
    /// 正解演出の開始状態を作る。
    /// </summary>
    private void PrepareCorrectAnimation()
    {
        if (correctRingBackRect != null)
        {
            correctRingBackRect.localScale =
                Vector3.one * 0.55f;
        }

        if (correctRingRect != null)
        {
            correctRingRect.localScale =
                Vector3.one * 0.55f;

            correctRingRect.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    -160f
                );
        }

        if (correctRotateLightRect != null)
        {
            correctRotateLightRect.localEulerAngles =
                Vector3.zero;
        }

        if (correctCountryImage != null)
        {
            RectTransform countryRect =
                correctCountryImage.rectTransform;

            countryRect.anchoredPosition =
                correctCountryPosition;

            countryRect.localScale =
                Vector3.one *
                correctCountryStartScale;

            countryRect.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    -12f
                );
        }

        if (correctTitleRect != null)
        {
            correctTitleRect.anchoredPosition =
                correctTitlePosition +
                new Vector2(
                    0f,
                    correctTitleStartOffsetY
                );

            correctTitleRect.localScale =
                Vector3.one * 0.75f;
        }

        if (correctCountryInfoPanel != null)
        {
            correctCountryInfoPanel.anchoredPosition =
                correctInfoPosition +
                new Vector2(
                    0f,
                    correctInfoStartOffsetY
                );

            correctCountryInfoPanel.localScale =
                Vector3.one * 0.9f;
        }

        if (correctCharacterImage != null)
        {
            RectTransform characterRect =
                correctCharacterImage.rectTransform;

            characterRect.anchoredPosition =
                correctCharacterPosition +
                new Vector2(-25f, -15f);

            characterRect.localScale =
                Vector3.one * 0.55f;

            characterRect.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    -8f
                );
        }

        if (correctCharacterIcons != null)
        {
            for (int i = 0;
                 i < correctCharacterIcons.Length;
                 i++)
            {
                Image icon =
                    correctCharacterIcons[i];

                if (icon == null)
                {
                    continue;
                }

                RectTransform iconRect =
                    icon.rectTransform;

                if (correctCharacterIconPositions != null &&
                    i < correctCharacterIconPositions.Length)
                {
                    iconRect.anchoredPosition =
                        correctCharacterIconPositions[i];
                }

                iconRect.localScale =
                    Vector3.one *
                    characterIconStartScale;

                float startRotation =
                    i % 2 == 0
                        ? -18f
                        : 18f;

                iconRect.localEulerAngles =
                    new Vector3(
                        0f,
                        0f,
                        startRotation
                    );

                Color iconColor =
                    icon.color;

                iconColor.a = 0f;
                icon.color =
                    iconColor;
            }
        }

    }


    /// <summary>
    /// 不正解演出の開始状態を作る。
    /// </summary>
    private void PrepareWrongAnimation()
    {
        if (wrongCountryImage != null)
        {
            RectTransform countryRect =
                wrongCountryImage.rectTransform;

            countryRect.anchoredPosition =
                wrongCountryPosition;

            countryRect.localScale =
                Vector3.one * 0.8f;

            countryRect.localEulerAngles =
                Vector3.zero;
        }

        if (wrongTitleRect != null)
        {
            wrongTitleRect.anchoredPosition =
                wrongTitlePosition +
                new Vector2(
                    0f,
                    wrongTitleStartOffsetY
                );

            wrongTitleRect.localScale =
                Vector3.one;
        }

        if (wrongCountryInfoPanel != null)
        {
            wrongCountryInfoPanel.anchoredPosition =
                wrongInfoPosition +
                new Vector2(
                    0f,
                    wrongInfoStartOffsetY
                );

            wrongCountryInfoPanel.localScale =
                Vector3.one;
        }
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

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (correctEffectRoot != null)
        {
            correctEffectRoot.SetActive(false);
        }

        if (wrongEffectRoot != null)
        {
            wrongEffectRoot.SetActive(false);
        }

        if (correctConfettiParticles != null)
        {
            foreach (ParticleSystem particle
                     in correctConfettiParticles)
            {
                if (particle == null)
                {
                    continue;
                }

                particle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }

        if (correctConfettiRoot != null)
        {
            correctConfettiRoot.SetActive(false);
        }
    }


    // ==================================================
    // 開発テスト用
    // ==================================================

    [ContextMenu("Test / Correct JPN")]
    private void TestCorrect()
    {
        Sprite flag = null;

        if (countryFlagDatabase != null)
        {
            flag =
                countryFlagDatabase.GetFlag(
                    "JPN"
                );
        }

        ShowCorrect(
            "JPN",
            flag,
            correctCountryImage != null
                ? correctCountryImage.sprite
                : null
        );
    }


    [ContextMenu("Test / Wrong JPN")]
    private void TestWrong()
    {
        Sprite flag = null;

        if (countryFlagDatabase != null)
        {
            flag =
                countryFlagDatabase.GetFlag(
                    "JPN"
                );
        }

        ShowWrong(
            "JPN",
            flag,
            wrongCountryImage != null
                ? wrongCountryImage.sprite
                : null
        );
    }
}