using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// ステージ選択画面の3ページを管理する。
///
/// ・地域ステージ
/// ・世界チャレンジ
/// ・ランダムチャレンジ
///
/// 左右ボタンで循環し、
/// 下部タブから直接移動する。
/// 上部タイトルもページに合わせてローカライズする。
/// </summary>
public class StageSelectPageController : MonoBehaviour
{
    private enum StagePage
    {
        Regional = 0,
        World = 1,
        Random = 2
    }


    [Header("上部タイトル")]

    [SerializeField]
    private LocalizeStringEvent titleLocalizeStringEvent;


    [Header("切り替えるページ")]

    [SerializeField]
    private GameObject regionalPage;

    [SerializeField]
    private GameObject worldPage;

    [SerializeField]
    private GameObject randomPage;


    [Header("タブ背景")]

    [SerializeField]
    private Image regionalTabImage;

    [SerializeField]
    private Image worldTabImage;

    [SerializeField]
    private Image randomTabImage;


    [Header("タブ文字")]

    [SerializeField]
    private TextMeshProUGUI regionalTabText;

    [SerializeField]
    private TextMeshProUGUI worldTabText;

    [SerializeField]
    private TextMeshProUGUI randomTabText;


    [Header("選択中の色")]

    [SerializeField]
    private Color selectedBackgroundColor =
        new Color32(
            25,
            118,
            181,
            255
        );

    [SerializeField]
    private Color selectedTextColor =
        new Color32(
            255,
            255,
            255,
            255
        );


    [Header("未選択の色")]

    [SerializeField]
    private Color normalBackgroundColor =
        new Color32(
            24,
            51,
            69,
            255
        );

    [SerializeField]
    private Color normalTextColor =
        new Color32(
            200,
            212,
            219,
            255
        );


    // 現在表示しているページ
    private StagePage currentPage;


    private void Start()
    {
        // 最初は地域ステージを表示
        ShowRegionalPage();
    }

    // ==================================================
    // 下部タブから直接開く
    // ==================================================

    public void ShowRegionalPage()
    {
        ShowPage(
            StagePage.Regional
        );
    }


    public void ShowWorldPage()
    {
        ShowPage(
            StagePage.World
        );
    }


    public void ShowRandomPage()
    {
        ShowPage(
            StagePage.Random
        );
    }


    // ==================================================
    // 左右矢印
    // ==================================================

    /// <summary>
    /// 右矢印。
    /// 次のページを表示する。
    /// </summary>
    public void ShowNextPage()
    {
        int nextIndex =
            ((int)currentPage + 1) % 3;

        ShowPage(
            (StagePage)nextIndex
        );
    }


    /// <summary>
    /// 左矢印。
    /// 前のページを表示する。
    /// </summary>
    public void ShowPreviousPage()
    {
        int previousIndex =
            ((int)currentPage - 1 + 3) % 3;

        ShowPage(
            (StagePage)previousIndex
        );
    }


    // ==================================================
    // 表示切替
    // ==================================================

    private void ShowPage(
        StagePage page)
    {
        currentPage = page;

        // 上部タイトルも現在のページに合わせて変更する
        UpdateTitle(
            page
        );

        bool showRegional =
            page == StagePage.Regional;

        bool showWorld =
            page == StagePage.World;

        bool showRandom =
            page == StagePage.Random;


        if (regionalPage != null)
        {
            regionalPage.SetActive(
                showRegional
            );
        }

        if (worldPage != null)
        {
            worldPage.SetActive(
                showWorld
            );
        }

        if (randomPage != null)
        {
            randomPage.SetActive(
                showRandom
            );
        }


        SetTabVisual(
            regionalTabImage,
            regionalTabText,
            showRegional
        );

        SetTabVisual(
            worldTabImage,
            worldTabText,
            showWorld
        );

        SetTabVisual(
            randomTabImage,
            randomTabText,
            showRandom
        );
    }


    // ==================================================
    // 上部タイトルの切り替え
    // ==================================================

    private void UpdateTitle(
        StagePage page)
    {
        if (titleLocalizeStringEvent == null)
        {
            Debug.LogWarning(
                "StageSelectPageController：Title Localize String Eventが設定されていません。",
                this
            );

            return;
        }

        string localizationKey;

        switch (page)
        {
            case StagePage.Regional:

                localizationKey =
                    "stage_Regional Stages";

                break;


            case StagePage.World:

                localizationKey =
                    "stage_World Challenges";

                break;


            case StagePage.Random:

                localizationKey =
                    "stage_RandomChallenge";

                break;


            default:

                localizationKey =
                    "stage_RandomChallenge";

                break;
        }


        titleLocalizeStringEvent
            .StringReference
            .SetReference(
                "UITexts",
                localizationKey
            );

        titleLocalizeStringEvent.RefreshString();
    }


    // ==================================================
    // タブの見た目
    // ==================================================

    private void SetTabVisual(
        Image tabImage,
        TextMeshProUGUI tabText,
        bool selected)
    {
        if (tabImage != null)
        {
            tabImage.color =
                selected
                    ? selectedBackgroundColor
                    : normalBackgroundColor;
        }

        if (tabText != null)
        {
            tabText.color =
                selected
                    ? selectedTextColor
                    : normalTextColor;
        }
    }
}