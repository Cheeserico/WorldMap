using DG.Tweening;
using TMPro;
using UnityEngine;


public class ResultPopup : PopupBase
{
    private StageData stageData;

    [Header("クリアタイム表示")]
    [SerializeField] private TextMeshProUGUI clearTimeText;
    [Header("ベストタイム表示")]
    [SerializeField] private TextMeshProUGUI bestTimeText;
    [Header("新記録表示")]
    [SerializeField] private RectTransform newRecordText;
    [Header("星評価")]
    [SerializeField] private GameObject[] yellowStars;
    private int currentStarCount;
    [Header("ボタン演出")]
    [SerializeField] private CanvasGroup retryButtonCanvasGroup;
    [SerializeField] private CanvasGroup titleButtonCanvasGroup;
    private bool currentIsNewRecord;

    // 演出側の入力制御のみ。広告・画面遷移はResultAdActionsが担当。
    private bool inputLocked;

    // App Reviewへ今回のクリアを通知済みか
    private bool hasNotifiedAppReview;

    private void Start()
    {
        LoadStageData();
    }

    public void SetResult(float clearTime, float bestTime, bool isNewRecord)
    {
        if (inputLocked) return;
        LoadStageData();
        currentIsNewRecord = isNewRecord;
        if (clearTimeText != null) clearTimeText.text = FormatTime(clearTime);
        if (bestTimeText != null) bestTimeText.text = FormatTime(bestTime);
        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(isNewRecord);
            if (isNewRecord) newRecordText.localScale = Vector3.zero;
        }
        int starCount = CalculateStarCount(clearTime);
        UpdateStars(starCount);
        PrepareButtons();

        PrepareButtons();

        if (!hasNotifiedAppReview)
        {
            hasNotifiedAppReview = true;

            if (AppReviewManager.Instance != null)
            {
                AppReviewManager.Instance.NotifyStageCleared();
            }
            else
            {
                Debug.LogWarning(
                    "[App Review] AppReviewManagerが見つかりません。",
                    this
                );
            }
        }
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int centiseconds = Mathf.FloorToInt((time - Mathf.Floor(time)) * 100f);
        return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }

    /// <summary>演出完了後にもロックが維持される、ボタン入力の共通制御。</summary>
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
        ApplyButtonInput(retryButtonCanvasGroup);
        ApplyButtonInput(titleButtonCanvasGroup);
    }

    private void ApplyButtonInput(CanvasGroup group)
    {
        if (group == null) return;
        group.interactable = !inputLocked && group.alpha >= 0.99f;
        group.blocksRaycasts = group.alpha > 0f;
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
        if (newRecordText == null || !newRecordText.gameObject.activeSelf) return;
        newRecordText.DOKill();
        newRecordText.localScale = Vector3.zero;
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.SetLink(gameObject);
        sequence.AppendInterval(1.3f);
        sequence.Append(newRecordText.DOScale(1.25f, 0.25f).SetEase(Ease.OutBack));
        sequence.Append(newRecordText.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));
    }

    private int CalculateStarCount(float clearTime)
    {
        if (stageData == null)
        {
            Debug.LogError("ResultPopupにStageDataが設定されていません。");
            return 1;
        }
        if (clearTime <= stageData.threeStarTime) return 3;
        if (clearTime <= stageData.twoStarTime) return 2;
        return 1;
    }

    private void UpdateStars(int starCount)
    {
        currentStarCount = starCount;
        if (yellowStars == null) return;
        for (int i = 0; i < yellowStars.Length; i++)
        {
            if (yellowStars[i] == null) continue;
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
        if (yellowStars == null) return;
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.SetLink(gameObject);
        sequence.AppendInterval(0.08f);
        for (int i = 0; i < currentStarCount; i++)
        {
            if (i >= yellowStars.Length) break;
            GameObject star = yellowStars[i];
            if (star == null) continue;
            Transform starTransform = star.transform;
            starTransform.DOKill();
            starTransform.localScale = Vector3.zero;
            sequence.AppendCallback(() =>
            {
                if (!inputLocked && SoundManager.Instance != null)
                    SoundManager.Instance.PlaySE(SEType.Star);
            });
            sequence.Append(starTransform.DOScale(1.25f, 0.18f).SetEase(Ease.OutBack));
            sequence.Append(starTransform.DOScale(1f, 0.08f).SetEase(Ease.OutQuad));
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
        if (canvasGroup == null) return;
        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.transform.localScale = Vector3.one * 0.9f;
    }

    private void PlayButtonAnimation()
    {
        float delay = GetButtonAnimationDelay();
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.SetLink(gameObject);
        sequence.AppendInterval(delay);
        AppendButtonAnimation(sequence, retryButtonCanvasGroup);
        sequence.AppendInterval(0.08f);
        AppendButtonAnimation(sequence, titleButtonCanvasGroup);
    }

    private void AppendButtonAnimation(Sequence sequence, CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;
        canvasGroup.DOKill();
        sequence.Append(canvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad));
        sequence.Join(canvasGroup.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        sequence.AppendCallback(() =>
        {
            if (canvasGroup != null)
            {
                // 広告待ちになった後、演出完了でボタンを再有効化しない。
                canvasGroup.interactable = !inputLocked;
                canvasGroup.blocksRaycasts = true;
            }
        });
    }

    private float GetButtonAnimationDelay()
    {
        float delay = 0.15f;
        delay += currentStarCount * 0.40f;
        if (currentIsNewRecord) delay += 0.55f;
        return delay;
    }

    private void LoadStageData()
    {
        CountryPuzzleManager puzzleManager = FindFirstObjectByType<CountryPuzzleManager>();
        if (puzzleManager == null)
        {
            Debug.LogError("CountryPuzzleManager が見つかりません。");
            return;
        }
        stageData = puzzleManager.CurrentStageData;
    }
}
