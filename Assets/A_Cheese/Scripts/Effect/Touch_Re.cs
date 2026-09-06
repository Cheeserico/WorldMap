using UnityEngine;

namespace MobileTouchEffectPack
{
    public class Touch_Re : MonoBehaviour
    {
        [Header("指の位置へ移動させるUIオブジェクト")]
        [SerializeField]
        private RectTransform touchEffectRoot;

        [Header("再生するパーティクル")]
        [SerializeField]
        private ParticleSystem touchParticle;

        [Header("押している間も指へ追従させる")]
        [SerializeField]
        private bool dragPlayMode = true;

        private RectTransform effectLayer;
        private Canvas rootCanvas;
        private Camera uiCamera;


        private void Awake()
        {
            if (touchEffectRoot == null)
            {
                Debug.LogError(
                    "Touch：Touch Effect Rootが設定されていません。",
                    this
                );

                return;
            }

            effectLayer =
                touchEffectRoot.parent as RectTransform;

            rootCanvas =
                touchEffectRoot.GetComponentInParent<Canvas>();

            if (rootCanvas != null &&
                rootCanvas.renderMode !=
                RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = rootCanvas.worldCamera;
            }
            else
            {
                // Screen Space - Overlayではnullを使用
                uiCamera = null;
            }
        }


        private void Update()
        {
            // タップ開始
            if (Input.GetMouseButtonDown(0))
            {
                MoveEffectToPointer();

                if (touchParticle != null)
                {
                    // 前回の粒を消して最初から再生
                    touchParticle.Stop(
                        true,
                        ParticleSystemStopBehavior
                            .StopEmittingAndClear
                    );

                    touchParticle.Play(true);
                }
            }

            // 押している間も指へ追従
            if (dragPlayMode &&
                Input.GetMouseButton(0))
            {
                MoveEffectToPointer();
            }

            // 指を離したら新しい粒の放出を止める
            if (Input.GetMouseButtonUp(0) &&
                touchParticle != null)
            {
                touchParticle.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmitting
                );
            }
        }


        private void MoveEffectToPointer()
        {
            if (touchEffectRoot == null ||
                effectLayer == null)
            {
                return;
            }

            bool converted =
                RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        effectLayer,
                        Input.mousePosition,
                        uiCamera,
                        out Vector2 localPoint
                    );

            if (converted)
            {
                touchEffectRoot.anchoredPosition =
                    localPoint;
            }
        }
    }
}