using DG.Tweening;
using UnityEngine;

public class LoadingEarthRotator : MonoBehaviour
{
    [Header("回転する地球")]
    [SerializeField]
    private RectTransform earthImage;

    [Header("1回転にかかる秒数")]
    [SerializeField]
    private float rotationDuration = 6f;

    [Header("時計回りにする")]
    [SerializeField]
    private bool clockwise = true;

    private Tween rotationTween;

    private void OnEnable()
    {
        StartRotation();
    }

    private void OnDisable()
    {
        StopRotation();
    }

    private void StartRotation()
    {
        if (earthImage == null)
        {
            earthImage = transform as RectTransform;
        }

        StopRotation();

        float targetAngle =
            clockwise ? -360f : 360f;

        rotationTween = earthImage
            .DORotate(
                new Vector3(0f, 0f, targetAngle),
                rotationDuration,
                RotateMode.FastBeyond360
            )
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true);
    }

    private void StopRotation()
    {
        if (rotationTween != null)
        {
            rotationTween.Kill();
            rotationTween = null;
        }
    }
}