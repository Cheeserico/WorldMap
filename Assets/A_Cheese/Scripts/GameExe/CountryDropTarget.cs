using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// CountrySlot用のドロップ判定エリア。
/// 自分と同じ国IDのピースだけCountrySlotへ渡す。
/// </summary>
public class CountryDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField]
    private CountrySlot countrySlot;

    public void OnDrop(PointerEventData eventData)
    {
        if (countrySlot == null)
        {
            return;
        }

        GameObject droppedObject = eventData.pointerDrag;

        if (droppedObject == null)
        {
            return;
        }

        CountryPieceDrag piece =
            droppedObject.GetComponent<CountryPieceDrag>();

        if (piece == null)
        {
            return;
        }

        // ★違う国なら何もしない
        if (piece.CountryId != countrySlot.CountryId)
        {
            return;
        }

        // 同じ国だけCountrySlotへ渡す
        countrySlot.OnDrop(eventData);
    }
}