using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 国画像の不透明な陸地部分に植物を生やし、
/// 少し表示してから消す正解演出。
/// </summary>
public class GrassGrowEffect : MonoBehaviour
{
    [Header("植物のPrefab")]
    [SerializeField]
    private RectTransform grassPrefab;

    [Header("生成数")]
    [Range(1, 10)]
    [SerializeField]
    private int grassCount = 3;

    [Header("大きさ")]
    [SerializeField]
    private Vector2 grassScaleRange =
        new Vector2(0.55f, 0.85f);

    [Header("伸びる時間")]
    [SerializeField]
    private float growDuration = 0.3f;

    [Header("植物ごとの時間差")]
    [SerializeField]
    private float interval = 0.08f;

    [Header("表示時間")]
    [SerializeField]
    private float displayDuration = 0.7f;

    [Header("消える時間")]
    [SerializeField]
    private float fadeDuration = 0.25f;

    [Header("陸地判定")]
    [Range(0f, 1f)]
    [SerializeField]
    private float alphaThreshold = 0.5f;

    [Tooltip("陸地を探す最大回数")]
    [SerializeField]
    private int maxSearchAttempts = 150;

    /// <summary>
    /// 国の陸地部分に植物を生やす。
    /// </summary>
    public void Play(
        Image countryImage,
        RectTransform countryImageRect
    )
    {
        if (
            grassPrefab == null ||
            countryImage == null ||
            countryImageRect == null ||
            countryImage.sprite == null
        )
        {
            return;
        }

        GameObject rootObject =
            new GameObject(
                "TemporaryGrassRoot",
                typeof(RectTransform)
            );

        RectTransform grassRoot =
            rootObject.GetComponent<RectTransform>();

        grassRoot.SetParent(
            countryImageRect,
            false
        );

        grassRoot.anchorMin =
            new Vector2(0.5f, 0.5f);

        grassRoot.anchorMax =
            new Vector2(0.5f, 0.5f);

        grassRoot.pivot =
            new Vector2(0.5f, 0.5f);

        grassRoot.anchoredPosition =
            Vector2.zero;

        grassRoot.sizeDelta =
            countryImageRect.rect.size;

        grassRoot.localScale =
            Vector3.one;

        grassRoot.SetAsLastSibling();

        int createdCount = 0;

        for (int i = 0; i < grassCount; i++)
        {
            if (
                TryGetLandPosition(
                    countryImage,
                    countryImageRect,
                    out Vector2 landPosition
                )
            )
            {
                CreateGrass(
                    grassRoot,
                    landPosition,
                    createdCount
                );

                createdCount++;
            }
        }

        if (createdCount == 0)
        {
            Destroy(rootObject);
            return;
        }

        float lifetime =
            growDuration +
            displayDuration +
            fadeDuration +
            (createdCount - 1) * interval +
            0.1f;

        DOVirtual.DelayedCall(
            lifetime,
            () =>
            {
                if (rootObject != null)
                {
                    Destroy(rootObject);
                }
            }
        ).SetLink(rootObject);
    }

    /// <summary>
    /// Spriteの不透明部分から配置場所を探す。
    /// </summary>
    private bool TryGetLandPosition(
        Image countryImage,
        RectTransform countryImageRect,
        out Vector2 localPosition
    )
    {
        localPosition = Vector2.zero;

        Sprite sprite =
            countryImage.sprite;

        Texture2D texture =
            sprite.texture;

        if (texture == null)
        {
            return false;
        }

        Rect textureRect =
            sprite.textureRect;

        for (
            int attempt = 0;
            attempt < maxSearchAttempts;
            attempt++
        )
        {
            float normalizedX =
                Random.Range(0.08f, 0.92f);

            float normalizedY =
                Random.Range(0.08f, 0.92f);

            int pixelX =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        textureRect.x +
                        textureRect.width *
                        normalizedX
                    ),
                    0,
                    texture.width - 1
                );

            int pixelY =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        textureRect.y +
                        textureRect.height *
                        normalizedY
                    ),
                    0,
                    texture.height - 1
                );

            Color pixel =
                texture.GetPixel(
                    pixelX,
                    pixelY
                );

            // 透明部分は海なので使わない
            if (pixel.a < alphaThreshold)
            {
                continue;
            }

            Rect imageRect =
                countryImageRect.rect;

            localPosition =
                new Vector2(
                    Mathf.Lerp(
                        imageRect.xMin,
                        imageRect.xMax,
                        normalizedX
                    ),
                    Mathf.Lerp(
                        imageRect.yMin,
                        imageRect.yMax,
                        normalizedY
                    )
                );

            return true;
        }

        return false;
    }

    private void CreateGrass(
        RectTransform grassRoot,
        Vector2 landPosition,
        int index
    )
    {
        RectTransform grass =
            Instantiate(
                grassPrefab,
                grassRoot
            );

        grass.gameObject.SetActive(true);

        grass.anchorMin =
            new Vector2(0.5f, 0.5f);

        grass.anchorMax =
            new Vector2(0.5f, 0.5f);

        // 下中央を根元にする
        grass.pivot =
            new Vector2(0.5f, 0f);

        grass.anchoredPosition =
            landPosition;

        grass.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                Random.Range(-12f, 12f)
            );

        float targetScale =
            Random.Range(
                grassScaleRange.x,
                grassScaleRange.y
            );

        grass.localScale =
            new Vector3(
                targetScale,
                0f,
                targetScale
            );

        CanvasGroup canvasGroup =
            grass.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                grass.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Sequence sequence =
            DOTween.Sequence();

        sequence.SetDelay(
            index * interval
        );

        sequence.Append(
            grass.DOScaleY(
                targetScale,
                growDuration
            )
            .SetEase(Ease.OutBack)
        );

        sequence.AppendInterval(
            displayDuration
        );

        sequence.Append(
            canvasGroup.DOFade(
                0f,
                fadeDuration
            )
        );

        sequence.SetLink(
            grass.gameObject
        );
    }
}