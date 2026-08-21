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

    [Header("正解・不正解表示")]
    [SerializeField]
    private AnswerFeedbackUI answerFeedbackUI;

    [Header("国旗データ")]
    [SerializeField]
    private CountryFlagDatabase countryFlagDatabase;



    // このスロットがすでに正解済みか
    private bool isOccupied;

    private void Awake()
    {
        if (answerFeedbackUI == null)
        {
            answerFeedbackUI =
                FindFirstObjectByType<AnswerFeedbackUI>();
        }

        if (countryFlagDatabase == null)
        {
            countryFlagDatabase =
                FindFirstObjectByType<CountryFlagDatabase>();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log(
            $"★ OnDropが呼ばれました：{gameObject.name}"
        );

        // すでに正解済みなら何もしない
        if (isOccupied)
        {
            Debug.Log(
                $"{gameObject.name} はすでに使用済みです。"
            );
            return;
        }

        GameObject droppedObject =
            eventData.pointerDrag;

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

        // ==================================================
        // 正解
        // ==================================================
        if (countryPiece.CountryId == countryId)
        {
            Debug.Log(
                $"{countryPiece.gameObject.name} は正解です。"
            );

            // 正解SE
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(
                    SEType.Correct
                );
            }

            // 正解表示
            if (answerFeedbackUI != null &&
                countryFlagDatabase != null)
            {
                Debug.Log(
                    "★ CorrectのFeedbackを呼びます"
                );

                Sprite flag =
                    countryFlagDatabase.GetFlag(
                        countryPiece.CountryId
                    );

                answerFeedbackUI.ShowCorrect(
                    countryPiece.CountryId,
                    flag
                );

            }
            else
            {
                Debug.LogWarning(
                    $"★ Correct Feedback参照なし " +
                    $"AnswerFeedbackUI={answerFeedbackUI} " +
                    $"CountryFlagDatabase={countryFlagDatabase}"
                );
            }

            RectTransform slotRect =
                transform as RectTransform;

            countryPiece.PlaceOnSlot(
                slotRect,
                placedCountryRoot
            );

            // このスロットを使用済みにする
            isOccupied = true;

            if (slotImage != null)
            {
                slotImage.enabled = false;
                slotImage.raycastTarget = false;
            }

            if (puzzleManager != null)
            {
                puzzleManager.AddPlacedCountry(
                    countryId
                );
            }

            // 正解した国をHintManagerへ通知
            HintManager hintManager =
                FindFirstObjectByType<HintManager>();

            if (hintManager != null)
            {
                hintManager.NotifyCountryPlaced(
                    countryId
                );
            }
        }
        // ==================================================
        // 不正解
        // ==================================================
        else
        {
            Debug.Log(
                $"{countryPiece.gameObject.name} は不正解です。" +
                $" ピースID：{countryPiece.CountryId}" +
                $" / スロットID：{countryId}"
            );

            // ここではWrong演出を出さない
            // CountryPieceDrag.OnEndDrag側で
            // 「最終的に正解しなかった」ときに出す
        }
    }

    /// <summary>
    /// 途中保存されていた配置済み国を復元する。
    /// 通常の正解演出やSE、進捗加算は行わない。
    /// </summary>
    public bool RestorePlacedCountry(
        CountryPieceDrag countryPiece
    )
    {
        if (countryPiece == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}：" +
                "復元するCountryPieceがありません。"
            );

            return false;
        }

        if (countryPiece.CountryId != countryId)
        {
            Debug.LogWarning(
                $"{gameObject.name}：" +
                $"国IDが一致しません。 " +
                $"Slot={countryId}, " +
                $"Piece={countryPiece.CountryId}"
            );

            return false;
        }

        if (placedCountryRoot == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}：" +
                "PlacedCountryRootが設定されていません。"
            );

            return false;
        }

        // すでに復元済みなら二重処理しない
        if (isOccupied)
        {
            return false;
        }

        RectTransform slotRect =
            transform as RectTransform;

        if (slotRect == null)
        {
            return false;
        }

        // ピースを演出なしで正しい位置へ配置
        countryPiece.RestoreOnSlot(
            slotRect,
            placedCountryRoot
        );

        // スロットを使用済みにする
        isOccupied = true;

        if (slotImage != null)
        {
            slotImage.enabled = false;
            slotImage.raycastTarget = false;
        }

        Debug.Log(
            $"{countryId}のSlotを途中データから復元しました。"
        );

        return true;
    }

    // StageDataに入っている国だけ白、それ以外を黄緑にする処理
    public void SetStageActive(bool isStageCountry)
    {
        if (slotImage == null)
        {
            return;
        }

        if (isStageCountry)
        {
            // 出題される国 → 白
            slotImage.color = Color.white;
        }
        else
        {
            // 出題されない国 → 黄緑 RGB(170,205,70)
            slotImage.color = new Color32(170, 205, 70, 255);
        }
    }
}