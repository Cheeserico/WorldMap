using DG.Tweening;
using TMPro;
using UnityEngine;

public class ResultPopup : PopupBase
{
    [Header("クリアタイム表示")]
    [SerializeField]
    private TextMeshProUGUI clearTimeText;

    [Header("ベストタイム表示")]
    [SerializeField]
    private TextMeshProUGUI bestTimeText;

    [Header("新記録表示")]
    [SerializeField]
    private RectTransform newRecordText;

    [Header("星評価")]
    [SerializeField]
    private GameObject[] yellowStars;

    [Header("星評価の時間設定")]
    [SerializeField]
    private float threeStarTime = 30f;

    [SerializeField]
    private float twoStarTime = 60f;

    private int currentStarCount;

    [Header("ボタン演出")]
    [SerializeField]
    private CanvasGroup retryButtonCanvasGroup;

    [SerializeField]
    private CanvasGroup titleButtonCanvasGroup;

    private bool currentIsNewRecord;

    public void SetResult(
        float clearTime,
        float bestTime,
        bool isNewRecord)
    {
        currentIsNewRecord = isNewRecord;

        if (clearTimeText != null)
        {
            clearTimeText.text =
                FormatTime(clearTime);
        }

        if (bestTimeText != null)
        {
            bestTimeText.text =
                FormatTime(bestTime);
        }

        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(
                isNewRecord
            );

            if (isNewRecord)
            {
                // Popupが開くまでは小さくして待機
                newRecordText.localScale =
                    Vector3.zero;
            }
        }
        int starCount = CalculateStarCount(clearTime);
        UpdateStars(starCount);

        PrepareButtons();
    }
    private string FormatTime(float time)
    {
        int minutes =
            Mathf.FloorToInt(time / 60f);

        int seconds =
            Mathf.FloorToInt(time % 60f);

        int centiseconds =
            Mathf.FloorToInt(
                (time - Mathf.Floor(time))
                * 100f
            );

        return
            $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }

    public void OnRetryButton()
    {
        Close();

        if (ResultManager.Instance != null)
        {
            ResultManager.Instance.Retry();
        }
    }

    public void OnTitleButton()
    {
        Close();

        if (ResultManager.Instance != null)
        {
            ResultManager.Instance.GoToTitle();
        }
    }

    protected override void OnAfterOpen()
    {
        base.OnAfterOpen();

        PlayStarAnimation();
        PlayNewRecordAnimation();
        PlayButtonAnimation();
    }

    private void PlayNewRecordAnimation()
    {
        if (newRecordText == null)
        {
            return;
        }

        if (!newRecordText.gameObject.activeSelf)
        {
            return;
        }

        newRecordText.DOKill();

        newRecordText.localScale =
            Vector3.zero;

        Sequence sequence =
            DOTween.Sequence()
            .SetUpdate(true);

        // Popupが開いたあと、少し間を置く
        sequence.AppendInterval(1.3f);

        // ポンッと大きくする
        sequence.Append(
            newRecordText
                .DOScale(1.25f, 0.25f)
                .SetEase(Ease.OutBack)
        );

        // 最後に通常サイズへ
        sequence.Append(
            newRecordText
                .DOScale(1f, 0.12f)
                .SetEase(Ease.OutQuad)
        );
    }

    private int CalculateStarCount(float clearTime)
    {
        if (clearTime <= threeStarTime)
        {
            return 3;
        }

        if (clearTime <= twoStarTime)
        {
            return 2;
        }

        return 1;
    }


    private void UpdateStars(int starCount)
    {
        currentStarCount = starCount;

        if (yellowStars == null)
        {
            return;
        }

        for (int i = 0; i < yellowStars.Length; i++)
        {
            if (yellowStars[i] == null)
            {
                continue;
            }

            bool isEarned = i < starCount;

            yellowStars[i].SetActive(isEarned);

            if (isEarned)
            {
                yellowStars[i].transform.DOKill();
                yellowStars[i].transform.localScale = Vector3.zero;
            }
        }
    }

    private void PlayStarAnimation()
    {
        if (yellowStars == null)
        {
            return;
        }

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true);

        // Popupが開いてから少し待つ
        sequence.AppendInterval(0.08f);

        for (int i = 0; i < currentStarCount; i++)
        {
            if (i >= yellowStars.Length)
            {
                break;
            }

            GameObject star = yellowStars[i];

            if (star == null)
            {
                continue;
            }

            Transform starTransform = star.transform;

            starTransform.DOKill();
            starTransform.localScale = Vector3.zero;

            // 星が出る直前にSE
            sequence.AppendCallback(() =>
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySE(SEType.Star);
                }
            });

            // ポンッと表示
            sequence.Append(
                starTransform
                    .DOScale(1.25f, 0.18f)
                    .SetEase(Ease.OutBack)
            );

            // 通常サイズへ
            sequence.Append(
                starTransform
                    .DOScale(1f, 0.08f)
                    .SetEase(Ease.OutQuad)
            );

            sequence.AppendInterval(0.04f);
        }
    }
    private void PrepareButtons()
    {
        PrepareButton(retryButtonCanvasGroup);
        PrepareButton(titleButtonCanvasGroup);
    }

    
    private void PrepareButton(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.DOKill();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        canvasGroup.transform.localScale =
            Vector3.one * 0.9f;
    }

    // ボタン演出
    private void PlayButtonAnimation()
    {
        float delay = GetButtonAnimationDelay();

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true);

        sequence.AppendInterval(delay);

        // Retry
        AppendButtonAnimation(
            sequence,
            retryButtonCanvasGroup
        );

        sequence.AppendInterval(0.08f);

        // Title
        AppendButtonAnimation(
            sequence,
            titleButtonCanvasGroup
        );
    }
    // 補助メソッド
    private void AppendButtonAnimation(
    Sequence sequence,
    CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.DOKill();

        sequence.Append(
            canvasGroup
                .DOFade(1f, 0.2f)
                .SetEase(Ease.OutQuad)
        );

        sequence.Join(
            canvasGroup.transform
                .DOScale(1f, 0.2f)
                .SetEase(Ease.OutBack)
        );

        sequence.AppendCallback(() =>
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        });
    }

    // 表示開始タイミング
    private float GetButtonAnimationDelay()
    {
        // 星演出開始まで
        float delay = 0.15f;

        // 星1個あたり
        delay += currentStarCount * 0.40f;

        // NEW RECORDがある場合は、その演出分も待つ
        if (currentIsNewRecord)
        {
            delay += 0.55f;
        }

        return delay;
    }
}