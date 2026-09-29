using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Result画面に、最後に正解した国に対応する
/// 地域キャラクターと3個の地域アイコンを表示する。
/// </summary>
public sealed class ResultCharacterCelebration : MonoBehaviour
{
    [Header("キャラクターデータ")]
    [SerializeField]
    private CountryCharacterDatabase countryCharacterDatabase;

    [Header("表示先")]
    [SerializeField]
    private Image characterImage;

    [Tooltip("キャラクター周辺から飛び出す3個のImage")]
    [SerializeField]
    private Image[] effectIconImages = new Image[3];

    [Header("キャラクター演出")]
    [SerializeField]
    private float characterStartScale = 0.7f;

    [SerializeField]
    private float characterAppearDuration = 0.3f;

    [SerializeField]
    private float characterPunchScale = 0.08f;

    [Header("地域アイコン演出")]
    [SerializeField]
    private Vector2[] iconMoveOffsets =
    {
        new Vector2(-100f, 90f),
        new Vector2(0f, 130f),
        new Vector2(100f, 90f)
    };

    [SerializeField]
    private float iconStartScale = 0.2f;

    [SerializeField]
    private float iconMoveDuration = 0.45f;

    [SerializeField]
    private float iconInterval = 0.08f;

    private Sequence currentSequence;
    private Vector3 characterOriginalScale;
    private Vector2[] iconOriginalPositions;

    private void Awake()
    {
        if (characterImage != null)
        {
            characterOriginalScale =
                characterImage.rectTransform.localScale;
        }

        if (effectIconImages != null)
        {
            iconOriginalPositions =
                new Vector2[effectIconImages.Length];

            for (int i = 0; i < effectIconImages.Length; i++)
            {
                Image icon = effectIconImages[i];

                if (icon == null)
                {
                    continue;
                }

                iconOriginalPositions[i] =
                    icon.rectTransform.anchoredPosition;
            }
        }

        HideIconsImmediately();
    }

    /// <summary>
    /// 国IDに対応するキャラクターと地域アイコンを表示する。
    /// </summary>
    public void Play(string countryId)
    {
        currentSequence?.Kill();

        ApplyCharacter(countryId);
        PrepareIcons(countryId);

        currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        PlayCharacterAnimation();
        PlayIconAnimations();
    }

    private void ApplyCharacter(string countryId)
    {
        if (characterImage == null)
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

        characterImage.sprite = characterSprite;
        characterImage.enabled = characterSprite != null;
        characterImage.preserveAspect = true;

        RectTransform characterRect =
            characterImage.rectTransform;

        characterRect.DOKill();
        characterRect.localScale =
            characterOriginalScale * characterStartScale;
    }

    private void PrepareIcons(string countryId)
    {
        Sprite[] effectIcons = null;

        if (countryCharacterDatabase != null)
        {
            effectIcons =
                countryCharacterDatabase.GetEffectIcons(
                    countryId
                );
        }

        if (effectIconImages == null)
        {
            return;
        }

        for (int i = 0; i < effectIconImages.Length; i++)
        {
            Image icon = effectIconImages[i];

            if (icon == null)
            {
                continue;
            }

            icon.DOKill();
            icon.rectTransform.DOKill();

            Sprite iconSprite =
                effectIcons != null && i < effectIcons.Length
                    ? effectIcons[i]
                    : null;

            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
            icon.preserveAspect = true;

            if (!icon.enabled)
            {
                continue;
            }

            icon.rectTransform.anchoredPosition =
                GetIconOriginalPosition(i);

            icon.rectTransform.localScale =
                Vector3.one * iconStartScale;

            icon.rectTransform.localRotation =
                Quaternion.identity;

            Color color = icon.color;
            color.a = 0f;
            icon.color = color;
        }
    }

    private void PlayCharacterAnimation()
    {
        if (characterImage == null ||
            !characterImage.enabled)
        {
            return;
        }

        RectTransform characterRect =
            characterImage.rectTransform;

        currentSequence.Insert(
            0f,
            characterRect
                .DOScale(
                    characterOriginalScale,
                    characterAppearDuration
                )
                .SetEase(Ease.OutBack)
        );

        currentSequence.Insert(
            characterAppearDuration,
            characterRect.DOPunchScale(
                Vector3.one * characterPunchScale,
                0.22f,
                5,
                0.5f
            )
        );
    }

    private void PlayIconAnimations()
    {
        if (effectIconImages == null)
        {
            return;
        }

        for (int i = 0; i < effectIconImages.Length; i++)
        {
            Image icon = effectIconImages[i];

            if (icon == null || !icon.enabled)
            {
                continue;
            }

            RectTransform iconRect =
                icon.rectTransform;

            float startTime =
                0.18f + i * iconInterval;

            Vector2 targetPosition =
                GetIconOriginalPosition(i) +
                GetIconMoveOffset(i);

            currentSequence.Insert(
                startTime,
                icon.DOFade(1f, 0.08f)
            );

            currentSequence.Insert(
                startTime,
                iconRect
                    .DOAnchorPos(
                        targetPosition,
                        iconMoveDuration
                    )
                    .SetEase(Ease.OutBack)
            );

            currentSequence.Insert(
                startTime,
                iconRect
                    .DOScale(1f, 0.22f)
                    .SetEase(Ease.OutBack)
            );

            float rotation =
                i % 2 == 0 ? 12f : -12f;

            currentSequence.Insert(
                startTime,
                iconRect
                    .DORotate(
                        new Vector3(0f, 0f, rotation),
                        iconMoveDuration
                    )
                    .SetEase(Ease.OutCubic)
            );

        }
    }

    private Vector2 GetIconOriginalPosition(int index)
    {
        if (iconOriginalPositions != null &&
            index >= 0 &&
            index < iconOriginalPositions.Length)
        {
            return iconOriginalPositions[index];
        }

        return Vector2.zero;
    }

    private Vector2 GetIconMoveOffset(int index)
    {
        if (iconMoveOffsets != null &&
            index >= 0 &&
            index < iconMoveOffsets.Length)
        {
            return iconMoveOffsets[index];
        }

        return Vector2.zero;
    }

    private void HideIconsImmediately()
    {
        if (effectIconImages == null)
        {
            return;
        }

        foreach (Image icon in effectIconImages)
        {
            if (icon == null)
            {
                continue;
            }

            Color color = icon.color;
            color.a = 0f;
            icon.color = color;
        }
    }

    private void OnDisable()
    {
        currentSequence?.Kill();
        HideIconsImmediately();
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
    }
}
