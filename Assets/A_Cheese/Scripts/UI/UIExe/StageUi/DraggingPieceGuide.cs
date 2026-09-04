using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 現在ドラッグしている国を、
/// 画面左下の固定パネルへ表示する。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DraggingPieceGuide : MonoBehaviour
{
    [Header("表示するUI")]
    [SerializeField]
    private Image countryImage;

    [SerializeField]
    private Image countryFlagImage;

    [SerializeField]
    private TMP_Text countryNameText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup =
            GetComponent<CanvasGroup>();

        Hide();
    }

    /// <summary>
    /// ドラッグ中の国を表示する。
    /// </summary>
    public void Show(
        Sprite countrySprite,
        Sprite flagSprite,
        string countryName
    )
    {
        if (countryImage != null)
        {
            countryImage.sprite =
                countrySprite;

            countryImage.preserveAspect =
                true;

            countryImage.enabled =
                countrySprite != null;
        }

        if (countryFlagImage != null)
        {
            countryFlagImage.sprite =
                flagSprite;

            countryFlagImage.preserveAspect =
                true;

            countryFlagImage.enabled =
                flagSprite != null;
        }

        if (countryNameText != null)
        {
            countryNameText.text =
                countryName;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    /// <summary>
    /// 案内を非表示にする。
    /// </summary>
    /// <summary>
    /// ドラッグしていないときの待機表示へ戻す。
    /// パネル自体は消さない。
    /// </summary>
    public void Hide()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}