using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// ヒントの地図移動とエフェクト表示を管理する。
/// </summary>
public class HintManager : MonoBehaviour
{
    [Header("地図操作")]
    [SerializeField]
    private MapPanZoomController mapPanZoomController;

    [Header("ヒントエフェクト")]
    [SerializeField]
    private RectTransform hintOverlay;

    [SerializeField]
    private RectTransform hintEffect;

    [SerializeField]
    private ParticleSystem hintParticle;

    [Header("ヒント選択案内")]
    [SerializeField]
    private TMP_Text hintGuideText;

    [SerializeField]
    private GameObject hintCancelButton;

    [Header("カード選択の手アニメーション")]
    [SerializeField]
    private RectTransform hintHandImage;

    [SerializeField]
    private float handMoveDistance = 120f;

    [SerializeField]
    private float handMoveDuration = 0.45f;

    [SerializeField]
    private float handTapDistance = 30f;

    [SerializeField]
    private float handTapDuration = 0.15f;

    [SerializeField]
    private float guideFadeDuration = 0.2f;

    [Header("1回目のヒント")]
    [SerializeField]
    private float firstHintMapScale = 1.5f;

    [SerializeField]
    private float firstHintEffectScale = 1.5f;

    [Header("2回目のヒント")]
    [SerializeField]
    private float secondHintMapScale = 2.2f;

    [SerializeField]
    private float secondHintEffectScale = 1f;

    [Header("3回目以降のヒント")]
    [SerializeField]
    private float thirdHintMapScale = 3f;

    [SerializeField]
    private float thirdHintEffectScale = 0.6f;

    [Header("表示時間")]
    [SerializeField]
    private float effectDuration = 3f;


    private RectTransform currentTarget;

    private string currentHintCountryId;

    private CountryPieceDrag currentHintPiece;

    private Tween hideTween;

    private Tween hintGuideTween;

    private Sequence hintHandSequence;

    private Vector2 hintHandStartPosition;
    private Vector3 hintHandStartScale;

    // 国IDごとのヒント使用回数
    private readonly Dictionary<string, int>
        hintUseCounts =
            new Dictionary<string, int>();

    // ヒントを見たいカードの選択中か
    private bool isSelectingCountry;

    public bool IsSelectingCountry =>
        isSelectingCountry;

    private void Awake()
    {
        if (hintEffect != null)
        {
            hintEffect.gameObject.SetActive(false);
        }

        if (hintGuideText != null)
        {
            hintGuideText.alpha = 0f;
            hintGuideText.gameObject.SetActive(false);
        }

        if (hintCancelButton != null)
        {
            hintCancelButton.SetActive(false);
        }

        if (hintHandImage != null)
        {
            hintHandStartPosition =
                hintHandImage.anchoredPosition;

            hintHandStartScale =
                hintHandImage.localScale;

            hintHandImage
                .gameObject
                .SetActive(false);
        }

    }

    private void LateUpdate()
    {
        if (currentTarget == null ||
            hintEffect == null ||
            !hintEffect.gameObject.activeSelf)
        {
            return;
        }

        // 地図を手動操作しても正解位置へ追従する
        hintEffect.position =
            currentTarget.position;
    }

    private void ShowHintHand()
    {
        if (hintHandImage == null)
        {
            return;
        }

        hintHandSequence?.Kill();

        hintHandImage.anchoredPosition =
            hintHandStartPosition;

        hintHandImage.localScale =
            hintHandStartScale;

        hintHandImage
            .gameObject
            .SetActive(true);

        float startX =
            hintHandStartPosition.x;

        float startY =
            hintHandStartPosition.y;

        hintHandSequence =
            DOTween.Sequence();

        // 左へ移動
        hintHandSequence.Append(
            hintHandImage
                .DOAnchorPosX(
                    startX - handMoveDistance,
                    handMoveDuration
                )
                .SetEase(Ease.InOutSine)
        );

        // 左から右へ移動
        hintHandSequence.Append(
            hintHandImage
                .DOAnchorPosX(
                    startX + handMoveDistance,
                    handMoveDuration * 2f
                )
                .SetEase(Ease.InOutSine)
        );

        // 中央へ戻る
        hintHandSequence.Append(
            hintHandImage
                .DOAnchorPosX(
                    startX,
                    handMoveDuration
                )
                .SetEase(Ease.InOutSine)
        );

        // カードへ指を下げてタップ
        hintHandSequence.Append(
            hintHandImage
                .DOAnchorPosY(
                    startY - handTapDistance,
                    handTapDuration
                )
                .SetEase(Ease.InQuad)
        );

        hintHandSequence.Join(
            hintHandImage
                .DOScale(
                    hintHandStartScale * 0.85f,
                    handTapDuration
                )
        );

        // 元の位置と大きさへ戻す
        hintHandSequence.Append(
            hintHandImage
                .DOAnchorPosY(
                    startY,
                    handTapDuration
                )
                .SetEase(Ease.OutQuad)
        );

        hintHandSequence.Join(
            hintHandImage
                .DOScale(
                    hintHandStartScale,
                    handTapDuration
                )
        );

        hintHandSequence.AppendInterval(
            0.4f
        );

        hintHandSequence.SetLoops(
            -1,
            LoopType.Restart
        );
    }

    private void HideHintHand()
    {
        hintHandSequence?.Kill();
        hintHandSequence = null;

        if (hintHandImage == null)
        {
            return;
        }

        hintHandImage.anchoredPosition =
            hintHandStartPosition;

        hintHandImage.localScale =
            hintHandStartScale;

        hintHandImage
            .gameObject
            .SetActive(false);
    }

    private void ShowHintGuide()
    {
        ShowHintHand();

        if (hintCancelButton != null)
        {
            hintCancelButton.SetActive(true);
        }

        if (hintGuideText == null)
        {
            return;
        }

        hintGuideTween?.Kill();

        hintGuideText.gameObject.SetActive(true);
        hintGuideText.alpha = 0f;

        hintGuideTween =
            hintGuideText
                .DOFade(
                    1f,
                    guideFadeDuration
                );
    }

    private void HideHintGuide()
    {
        HideHintHand();

        if (hintCancelButton != null)
        {
            hintCancelButton.SetActive(false);
        }

        if (hintGuideText == null)
        {
            return;
        }

        hintGuideTween?.Kill();

        hintGuideTween =
            hintGuideText
                .DOFade(
                    0f,
                    guideFadeDuration
                )
                .OnComplete(() =>
                {
                    hintGuideText
                        .gameObject
                        .SetActive(false);
                });
    }

    /// <summary>
    /// HintButtonから呼び出す。
    /// ヒント対象カードの選択モードを開始する。
    /// </summary>
    public void BeginHintSelection()
    {
        // 選択中にもう一度押した場合はキャンセル
        if (isSelectingCountry)
        {
            CancelHintSelection();
            return;
        }

        StopHintEffect();

        isSelectingCountry = true;

        ShowHintGuide();

        Debug.Log(
            "ヒントを見たい国カードを選んでください。"
        );
    }

    /// <summary>
    /// ヒント対象の選択をキャンセルする。
    /// </summary>
    public void CancelHintSelection()
    {
        isSelectingCountry = false;

        HideHintGuide();

        Debug.Log(
            "ヒント選択をキャンセルしました。"
        );
    }

    /// <summary>
    /// CountryPieceDragから、選択した国を受け取る。
    /// </summary>
    public void SelectCountryForHint(
        CountryPieceDrag countryPiece
    )
    {
        if (!isSelectingCountry ||
            countryPiece == null)
        {
            return;
        }

        string countryId =
            countryPiece.CountryId;

        GameObject slotObject =
            GameObject.Find(
                countryId + "_Slot"
            );

        if (slotObject == null)
        {
            Debug.LogWarning(
                $"{countryId}_Slotが見つかりません。"
            );

            return;
        }

        Transform dropTargetTransform =
            slotObject.transform.Find(
                "DropTarget"
            );

        RectTransform dropTarget =
            dropTargetTransform
                as RectTransform;

        if (dropTarget == null)
        {
            Debug.LogWarning(
                $"{countryId}のDropTargetが見つかりません。"
            );

            return;
        }

        int useCount = 0;

        hintUseCounts.TryGetValue(
            countryId,
            out useCount
        );

        useCount++;

        hintUseCounts[countryId] =
            useCount;

        float mapScale;
        float effectScale;

        if (useCount == 1)
        {
            mapScale =
                firstHintMapScale;

            effectScale =
                firstHintEffectScale;
        }
        else if (useCount == 2)
        {
            mapScale =
                secondHintMapScale;

            effectScale =
                secondHintEffectScale;
        }
        else
        {
            mapScale =
                thirdHintMapScale;

            effectScale =
                thirdHintEffectScale;
        }

        isSelectingCountry = false;

        HideHintGuide();

        hintEffect.localScale =
            new Vector3(
                effectScale,
                effectScale,
                1f
            );

        Debug.Log(
            $"ヒント対象：{countryId} / " +
            $"{useCount}回目"
        );

        ShowHint(
            dropTarget,
            mapScale
        );

        currentHintCountryId =
            countryId;

        currentHintPiece =
            countryPiece;

        currentHintPiece.PlayHintHighlight();
    }


    /// <summary>
    /// 指定したDropTargetへ移動し、
    /// ヒントエフェクトを表示する。
    /// </summary>
    public void ShowHint(
        RectTransform dropTarget,
        float targetScale
    )
    {
        if (dropTarget == null ||
            mapPanZoomController == null ||
            hintEffect == null)
        {
            Debug.LogWarning(
                "HintManagerの参照が不足しています。"
            );

            return;
        }

        StopHintEffect();

        currentTarget = dropTarget;

        mapPanZoomController.FocusOnTarget(
            dropTarget,
            targetScale,
            PlayHintEffect
        );
    }

    private void PlayHintEffect()
    {
        if (currentTarget == null)
        {
            return;
        }

        hintEffect.position =
            currentTarget.position;

        hintEffect.gameObject.SetActive(true);

        if (hintParticle != null)
        {
            hintParticle.Clear(true);
            hintParticle.Play(true);
        }

        hideTween?.Kill();

        hideTween =
            DOVirtual.DelayedCall(
                effectDuration,
                StopHintEffect
            );
    }

    /// <summary>
    /// 国が正解配置されたときに呼ばれる。
    /// 同じ国のヒント表示中なら即終了する。
    /// </summary>
    public void NotifyCountryPlaced(
        string countryId
    )
    {
        if (string.IsNullOrEmpty(countryId))
        {
            return;
        }

        // 配置済みになった国の回数記録は不要
        hintUseCounts.Remove(
            countryId
        );

        if (currentHintCountryId ==
            countryId)
        {
            StopHintEffect();
        }
    }

    /// <summary>
    /// Pause・Result・シーン移動時などに、
    /// ヒント関連の処理をすべて終了する。
    /// </summary>
    public void StopAllHints()
    {
        HideHintHand();

        if (hintCancelButton != null)
        {
            hintCancelButton.SetActive(false);
        }

        isSelectingCountry = false;

        if (mapPanZoomController != null)
        {
            mapPanZoomController
                .CancelHintFocus();
        }

        hintGuideTween?.Kill();
        hintGuideTween = null;

        if (hintGuideText != null)
        {
            hintGuideText.alpha = 0f;

            hintGuideText
                .gameObject
                .SetActive(false);
        }

        StopHintEffect();
    }

    public void StopHintEffect()
    {
        hideTween?.Kill();
        hideTween = null;

        if (hintParticle != null)
        {
            hintParticle.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear
            );
        }

        if (hintEffect != null)
        {
            hintEffect.gameObject.SetActive(false);
        }

        if (currentHintPiece != null)
        {
            currentHintPiece.StopHintHighlight();
        }

        currentHintPiece = null;

        currentTarget = null;

        currentHintCountryId = null;
    }


    private void OnDestroy()
    {
        hideTween?.Kill();
        hintGuideTween?.Kill();
        hintHandSequence?.Kill();
    }

}