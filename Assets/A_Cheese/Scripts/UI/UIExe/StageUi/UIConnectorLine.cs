using UnityEngine;

/// <summary>
/// UI上のステージカードと地図上のマーカーを
/// 直線で結ぶ。
///
/// カード・マーカー・ラインが別々の親にいても、
/// ワールド座標から変換して自動追従する。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIConnectorLine : MonoBehaviour
{
    [Header("接続対象")]

    [Tooltip("接続するステージカード")]
    [SerializeField]
    private RectTransform stageCard;

    [Tooltip("地図上の地点マーカー")]
    [SerializeField]
    private RectTransform mapMarker;


    [Header("線の設定")]

    [SerializeField]
    [Min(1f)]
    private float lineThickness = 6f;


    private RectTransform lineRectTransform;
    private RectTransform lineParent;


    private void Awake()
    {
        Initialize();
        UpdateLine();
    }


    private void OnEnable()
    {
        Initialize();
        UpdateLine();
    }


    /// <summary>
    /// Unity Editor上で値を変更したときにも更新する。
    /// </summary>
    private void OnValidate()
    {
        Initialize();
        UpdateLine();
    }


    /// <summary>
    /// ラインが別の親へ移動されたとき、
    /// 新しい親を取得し直す。
    /// </summary>
    private void OnTransformParentChanged()
    {
        Initialize();
        UpdateLine();
    }


    private void LateUpdate()
    {
        UpdateLine();
    }


    private void Initialize()
    {
        if (lineRectTransform == null)
        {
            lineRectTransform =
                GetComponent<RectTransform>();
        }

        // 現在の親を毎回取得する。
        // LineOverlayへ移動した場合にも対応できる。
        if (lineRectTransform != null)
        {
            lineParent =
                lineRectTransform.parent
                    as RectTransform;
        }
    }


    private void UpdateLine()
    {
        if (lineRectTransform == null ||
            lineParent == null ||
            stageCard == null ||
            mapMarker == null)
        {
            return;
        }


        // カードのワールド座標を、
        // LineOverlay内のローカル座標へ変換する。
        Vector2 cardPosition =
            lineParent.InverseTransformPoint(
                stageCard.position
            );


        // マーカーのワールド座標を、
        // LineOverlay内のローカル座標へ変換する。
        Vector2 markerPosition =
            lineParent.InverseTransformPoint(
                mapMarker.position
            );


        Vector2 direction =
            cardPosition -
            markerPosition;

        float distance =
            direction.magnitude;


        if (distance <= 0.01f)
        {
            return;
        }


        Vector2 centerPosition =
            (
                cardPosition +
                markerPosition
            ) * 0.5f;


        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;


        lineRectTransform.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        lineRectTransform.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        lineRectTransform.pivot =
            new Vector2(
                0.5f,
                0.5f
            );


        lineRectTransform.anchoredPosition =
            centerPosition;


        lineRectTransform.sizeDelta =
            new Vector2(
                distance,
                lineThickness
            );


        lineRectTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }
}