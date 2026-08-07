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


    public void SetResult(
        float clearTime,
        float bestTime,
        bool isNewRecord)
    {
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

        PlayNewRecordAnimation();
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
        sequence.AppendInterval(0.15f);

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
}