using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 国ピースを受け取るスロット。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CountrySlot : MonoBehaviour, IDropHandler
{
    [Header("このスロットの国ID")]
    [SerializeField]
    private string countryId;

    public string CountryId => countryId;

    [Header("配置済みピースをまとめる親")]
    [SerializeField]
    private RectTransform placedCountryRoot;

    [Header("正解後に非表示にするスロット画像")]
    [SerializeField]
    private Image slotImage;

    [Header("ゲーム進行管理")]
    [SerializeField]
    private CountryPuzzleManager puzzleManager;



    // このスロットがすでに正解済みか
    private bool isOccupied;

    public void OnDrop(PointerEventData eventData)
    {
        // すでに正解済みなら何もしない
        if (isOccupied)
        {
            Debug.Log($"{gameObject.name} はすでに使用済みです。");
            return;
        }

        GameObject droppedObject = eventData.pointerDrag;

        if (droppedObject == null)
        {
            return;
        }

        CountryPieceDrag countryPiece =
            droppedObject.GetComponent<CountryPieceDrag>();

        if (countryPiece == null)
        {
            return;
        }

        // ピースとスロットの国IDを比較
        if (countryPiece.CountryId == countryId)
        {
            Debug.Log(
                $"{countryPiece.gameObject.name} は正解です。"
            );

            // 正解SE
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(SEType.Correct);
            }


            RectTransform slotRect = transform as RectTransform;

            countryPiece.PlaceOnSlot(
                slotRect,
                placedCountryRoot
            );

            // このスロットを使用済みにする
            isOccupied = true;

            if (slotImage != null)
            {
                slotImage.enabled = false;
            }

            if (slotImage != null)
            {
                slotImage.enabled = false;
                slotImage.raycastTarget = false;
            }

            if (puzzleManager != null)
            {
                puzzleManager.AddPlacedCountry();
            }
        }
        else
        {
            Debug.Log(
                $"{countryPiece.gameObject.name} は不正解です。" +
                $" ピースID：{countryPiece.CountryId}" +
                $" / スロットID：{countryId}"
            );

            // 不正解SE
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(SEType.Wrong);
            }

        }
    }
}