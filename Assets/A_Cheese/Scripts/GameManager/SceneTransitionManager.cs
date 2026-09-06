using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全シーン共通のLoading表示と、
/// 非同期シーン移動を管理する。
///
/// ・背景はボタンを押した直後に表示
/// ・一定時間を超えた場合だけSpinnerと文字を表示
/// ・表示中は画面操作を遮断
/// </summary>
[DisallowMultipleComponent]
public sealed class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance
    {
        get;
        private set;
    }

    [Header("Loading UI")]

    [Tooltip("LoadingCanvas全体のCanvasGroup")]
    [SerializeField]
    private CanvasGroup loadingCanvasGroup;

    [Tooltip("SpinnerとLoading文字をまとめたCanvasGroup")]
    [SerializeField]
    private CanvasGroup loadingIndicatorCanvasGroup;

    [SerializeField]
    private RectTransform loadingSpinner;

    [Header("表示設定")]

    [Tooltip("Loadingアイコンの回転速度")]
    [SerializeField]
    private float spinnerRotationSpeed = 240f;

    [Tooltip("この秒数を超えた場合だけSpinnerと文字を表示する")]
    [SerializeField]
    private float indicatorDisplayDelay = 0.5f;

    [Tooltip("Spinnerが表示された場合の最低表示時間")]
    [SerializeField]
    private float minimumIndicatorDisplayDuration = 0.4f;

    private bool isLoading;
    private bool isIndicatorVisible;

    public static bool IsLoading =>
        Instance != null &&
        Instance.isLoading;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(
            gameObject
        );

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.gameObject.SetActive(
                true
            );
        }

        HideEverything();
    }

    private void Update()
    {
        if (
            !isLoading ||
            !isIndicatorVisible ||
            loadingSpinner == null
        )
        {
            return;
        }

        loadingSpinner.Rotate(
            0f,
            0f,
            -spinnerRotationSpeed *
            Time.unscaledDeltaTime
        );
    }

    /// <summary>
    /// Loading付きで指定シーンへ移動する。
    /// </summary>
    public static void Load(
        string sceneName
    )
    {
        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            Debug.LogError(
                "SceneTransitionManager：" +
                "移動先Scene名が空です。"
            );

            return;
        }

        if (Instance == null)
        {
            Debug.LogWarning(
                "SceneTransitionManagerがないため、" +
                "通常のScene読み込みを行います。"
            );

            SceneManager.LoadScene(
                sceneName
            );

            return;
        }

        Instance.BeginLoad(
            sceneName
        );
    }

    /// <summary>
    /// 現在のシーンをLoading付きで再読み込みする。
    /// </summary>
    public static void ReloadCurrentScene()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();

        Load(
            currentScene.name
        );
    }

    private void BeginLoad(
        string sceneName
    )
    {
        // 連打による二重読み込みを防止
        if (isLoading)
        {
            return;
        }

        StartCoroutine(
            LoadSceneRoutine(
                sceneName
            )
        );
    }

    private IEnumerator LoadSceneRoutine(
        string sceneName
    )
    {
        isLoading = true;
        isIndicatorVisible = false;

        float loadingStartedAt =
            Time.unscaledTime;

        float indicatorShownAt = 0f;

        /*
         * 最初は何も表示せず、
         * 透明な状態で入力だけ遮断する。
         */
        BlockInputWithoutShowing();
        HideIndicator();

        yield return null;

        AsyncOperation loadOperation = null;

        try
        {
            loadOperation =
                SceneManager.LoadSceneAsync(
                    sceneName
                );
        }
        catch (System.Exception error)
        {
            Debug.LogException(
                error,
                this
            );
        }

        if (loadOperation == null)
        {
            isLoading = false;
            HideEverything();
            yield break;
        }

        while (!loadOperation.isDone)
        {
            /*
             * 0.5秒を超えても読み込み中なら、
             * SpinnerとLoading文字を表示する。
             */
            if (
                !isIndicatorVisible &&
                Time.unscaledTime -
                loadingStartedAt >=
                indicatorDisplayDelay
            )
            {
                isIndicatorVisible = true;

                indicatorShownAt =
                    Time.unscaledTime;

                // 0.5秒を超えたら、
                // 背景・Spinner・文字をまとめて表示
                ShowFullLoading();
            }

            yield return null;
        }

        /*
         * 新しいシーンのAwake・Startなどが
         * 処理される余裕を持たせる。
         */
        yield return null;
        yield return null;

        /*
         * Spinnerが一度表示された場合は、
         * 一瞬の点滅を防ぐため最低時間を確保する。
         */
        if (isIndicatorVisible)
        {
            while (
                Time.unscaledTime -
                indicatorShownAt <
                minimumIndicatorDisplayDuration
            )
            {
                yield return null;
            }
        }

        isLoading = false;
        isIndicatorVisible = false;

        HideEverything();
    }

    /// <summary>
    /// 背景だけ即時表示して操作を遮断する。
    /// </summary>
    /// <summary>
    /// 見た目は表示せず、入力だけ遮断する。
    /// </summary>
    private void BlockInputWithoutShowing()
    {
        if (loadingCanvasGroup == null)
        {
            return;
        }

        loadingCanvasGroup.alpha = 0f;
        loadingCanvasGroup.blocksRaycasts = true;
        loadingCanvasGroup.interactable = true;

        loadingCanvasGroup.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 背景・Spinner・文字をすべて表示する。
    /// </summary>
    private void ShowFullLoading()
    {
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 1f;
            loadingCanvasGroup.blocksRaycasts = true;
            loadingCanvasGroup.interactable = true;

            loadingCanvasGroup.transform.SetAsLastSibling();
        }

        ShowIndicator();

        Canvas.ForceUpdateCanvases();
    }

    private void ShowIndicator()
    {
        if (loadingIndicatorCanvasGroup == null)
        {
            return;
        }

        loadingIndicatorCanvasGroup.alpha = 1f;
        loadingIndicatorCanvasGroup.blocksRaycasts = false;
        loadingIndicatorCanvasGroup.interactable = false;
    }

    private void HideIndicator()
    {
        if (loadingIndicatorCanvasGroup == null)
        {
            return;
        }

        loadingIndicatorCanvasGroup.alpha = 0f;
        loadingIndicatorCanvasGroup.blocksRaycasts = false;
        loadingIndicatorCanvasGroup.interactable = false;
    }

    private void HideEverything()
    {
        HideIndicator();

        if (loadingCanvasGroup == null)
        {
            return;
        }

        loadingCanvasGroup.alpha = 0f;
        loadingCanvasGroup.blocksRaycasts = false;
        loadingCanvasGroup.interactable = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}