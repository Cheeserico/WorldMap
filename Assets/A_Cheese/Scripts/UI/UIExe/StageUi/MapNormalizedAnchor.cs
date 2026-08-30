using UnityEngine;

/// <summary>
/// 地図上のUIを、MapContent内の割合で配置する。
///
/// X = 0：地図の左端
/// X = 1：地図の右端
/// Y = 0：地図の下端
/// Y = 1：地図の上端
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class MapNormalizedAnchor : MonoBehaviour
{
    [Header("地図内の位置")]

    [Tooltip(
        "Xは左0～右1、Yは下0～上1"
    )]
    [SerializeField]
    private Vector2 normalizedPosition =
        new Vector2(0.5f, 0.5f);

    private RectTransform rectTransform;


    private void Awake()
    {
        ApplyPosition();
    }


    private void OnEnable()
    {
        ApplyPosition();
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        // OnValidate中にRectTransformを変更すると
        // Unity UIのSendMessageエラーが出るため、
        // 次のEditor更新タイミングまで遅らせる。
        UnityEditor.EditorApplication.delayCall -=
            ApplyPositionFromEditor;

        UnityEditor.EditorApplication.delayCall +=
            ApplyPositionFromEditor;
    }


    private void ApplyPositionFromEditor()
    {
        if (this == null ||
            !isActiveAndEnabled)
        {
            return;
        }

        ApplyPosition();
    }

#endif


    [ContextMenu("Apply Position")]
    public void ApplyPosition()
    {
        if (rectTransform == null)
        {
            rectTransform =
                GetComponent<RectTransform>();
        }

        if (rectTransform == null)
        {
            return;
        }


        Vector2 clampedPosition =
            new Vector2(
                Mathf.Clamp01(
                    normalizedPosition.x
                ),
                Mathf.Clamp01(
                    normalizedPosition.y
                )
            );


        rectTransform.anchorMin =
            clampedPosition;

        rectTransform.anchorMax =
            clampedPosition;

        rectTransform.anchoredPosition =
            Vector2.zero;
    }
}