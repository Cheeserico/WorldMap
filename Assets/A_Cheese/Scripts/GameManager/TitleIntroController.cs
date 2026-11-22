
using System.Collections;
using UnityEngine;

public class TitleIntroController : MonoBehaviour
{
    [Header("タイトル画像")]
    [SerializeField]
    private GameObject titlePopup;

    [SerializeField]
    private CanvasGroup titleCanvasGroup;

    [Header("表示時間")]
    [SerializeField]
    private float displayDuration = 2f;

    [SerializeField]
    private float fadeDuration = 0.4f;

    // アプリ起動中に一度だけ表示
    private static bool hasShownIntro = false;

    private void Awake()
    {
        if (titlePopup == null ||
            titleCanvasGroup == null)
        {
            Debug.LogError(
                "TitleIntroController：参照が設定されていません。",
                this
            );
            return;
        }

        if (hasShownIntro)
        {
            titlePopup.SetActive(false);
            return;
        }

        // 最初からタイトルを表示
        titlePopup.SetActive(true);
        titleCanvasGroup.alpha = 1f;
        titleCanvasGroup.blocksRaycasts = true;
        titleCanvasGroup.interactable = true;
    }


    private void Start()
    {
        if (hasShownIntro ||
            titlePopup == null ||
            titleCanvasGroup == null)
        {
            return;
        }

        hasShownIntro = true;

        // タイトル表示時にSEを1回再生
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(
                SEType.Win
            );
        }

        StartCoroutine(ShowIntroRoutine());
    }
    private IEnumerator ShowIntroRoutine()
    {
        // タイトルを一定時間表示
        yield return new WaitForSecondsRealtime(
            displayDuration
        );

        float elapsed = 0f;

        // 徐々に透明にする
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            titleCanvasGroup.alpha =
                1f - Mathf.Clamp01(
                    elapsed / fadeDuration
                );

            yield return null;
        }

        titleCanvasGroup.alpha = 0f;
        titleCanvasGroup.blocksRaycasts = false;
        titleCanvasGroup.interactable = false;

        titlePopup.SetActive(false);
    }
}
