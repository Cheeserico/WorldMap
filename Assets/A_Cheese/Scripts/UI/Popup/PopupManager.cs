using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hierarchy上に常駐しているPopupを一括管理する。
///
/// ・PopupTypeによる開閉
/// ・二重表示防止
/// ・表示順の管理
/// ・最前面Popupの管理
///
/// を担当する。
///
/// ゲーム停止やシーン移動など、
/// Popup固有のゲーム処理は担当しない。
/// </summary>
public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    /// <summary>
    /// InspectorでPopupTypeとPopup本体を対応させるためのデータ。
    /// </summary>
    [Serializable]
    private class PopupEntry
    {
        [Tooltip("Popupの種類")]
        public PopupType popupType;

        [Tooltip("Hierarchy上に配置してあるPopup")]
        public PopupBase popup;
    }

    [Header("管理するPopup一覧")]
    [SerializeField]
    private List<PopupEntry> popupEntries
        = new List<PopupEntry>();

    /*
     * PopupTypeからPopupを検索するためのDictionary。
     *
     * 毎回Listを最初から検索せず、
     * PopupTypeを使ってすぐ取得できる。
     */
    private readonly Dictionary<PopupType, PopupBase> popupDictionary
        = new Dictionary<PopupType, PopupBase>();

    /*
     * Popupを開いた順番で保持する。
     *
     * 一番最後の要素が、
     * 現在最前面にあるPopup。
     */
    private readonly List<PopupType> openedPopupStack
        = new List<PopupType>();

    /// <summary>
    /// 現在1つ以上Popupが開いているか。
    /// </summary>
    public bool HasOpenPopup => openedPopupStack.Count > 0;

    /// <summary>
    /// 現在開いているPopup数。
    /// </summary>
    public int OpenPopupCount => openedPopupStack.Count;

    private void Awake()
    {
        InitializeSingleton();
        BuildPopupDictionary();
    }

    /// <summary>
    /// Singletonを初期化する。
    /// </summary>
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "PopupManagerが複数存在するため、重複した方を破棄します。",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// InspectorのListからDictionaryを作成する。
    /// </summary>
    private void BuildPopupDictionary()
    {
        popupDictionary.Clear();
        openedPopupStack.Clear();

        foreach (PopupEntry entry in popupEntries)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.popup == null)
            {
                Debug.LogWarning(
                    $"PopupManager：{entry.popupType}のPopupが設定されていません。",
                    this
                );

                continue;
            }

            if (popupDictionary.ContainsKey(entry.popupType))
            {
                Debug.LogWarning(
                    $"PopupManager：{entry.popupType}が重複登録されています。",
                    this
                );

                continue;
            }

            popupDictionary.Add(
                entry.popupType,
                entry.popup
            );
        }
    }

    /// <summary>
    /// InspectorのPauseButtonから呼び出すためのメソッド。
    /// </summary>
    public void OpenPause()
    {
        Open(PopupType.Pause);
    }

    /// <summary>
    /// InspectorのResumeButtonから呼び出すためのメソッド。
    /// 後でPauseManagerへ置き換える。
    /// </summary>
    public void ClosePause()
    {
        Close(PopupType.Pause);
    }

    /// <summary>
    /// 指定したPopupを表示する。
    /// </summary>
    public void Open(PopupType popupType)
    {
        if (!TryGetPopup(
                popupType,
                out PopupBase popup))
        {
            return;
        }

        /*
         * すでに開いている場合は、
         * 再度アニメーションさせず最前面へ移動する。
         */
        if (popup.IsOpen || popup.IsTransitioning)
        {
            MoveToFront(
                popupType,
                popup
            );

            return;
        }

        MoveToFront(
            popupType,
            popup
        );

        popup.Open();
    }

    /// <summary>
    /// 指定したPopupを閉じる。
    /// </summary>
    public void Close(PopupType popupType)
    {
        if (!TryGetPopup(
                popupType,
                out PopupBase popup))
        {
            return;
        }

        /*
         * 開いておらず、
         * 開閉アニメーション中でもないなら何もしない。
         */
        if (!popup.IsOpen &&
            !popup.IsTransitioning)
        {
            RemoveFromStack(popupType);
            return;
        }

        /*
         * PopupBaseのClosedイベントへ、
         * Stackから削除する処理を一度だけ登録する。
         */
        void HandleClosed()
        {
            popup.Closed -= HandleClosed;
            RemoveFromStack(popupType);
        }

        popup.Closed += HandleClosed;
        popup.Close();
    }

    /// <summary>
    /// 現在一番手前にあるPopupを閉じる。
    ///
    /// Androidの戻るボタンなどにも使える。
    /// </summary>
    public void CloseTop()
    {
        RemoveInvalidStackEntries();

        if (openedPopupStack.Count == 0)
        {
            return;
        }

        PopupType topPopupType =
            openedPopupStack[openedPopupStack.Count - 1];

        Close(topPopupType);
    }

    /// <summary>
    /// 開いているPopupをすべて閉じる。
    /// </summary>
    public void CloseAll()
    {
        RemoveInvalidStackEntries();

        /*
         * Close処理中にStackが変化するため、
         * コピーを取って後ろから閉じる。
         */
        PopupType[] openedPopupCopies =
            openedPopupStack.ToArray();

        for (int i = openedPopupCopies.Length - 1;
             i >= 0;
             i--)
        {
            Close(openedPopupCopies[i]);
        }
    }

    /// <summary>
    /// 指定したPopupが現在開いているか確認する。
    /// </summary>
    public bool IsOpen(PopupType popupType)
    {
        if (!TryGetPopup(
                popupType,
                out PopupBase popup))
        {
            return false;
        }

        return popup.IsOpen;
    }

    /// <summary>
    /// 指定したPopupが開閉途中か確認する。
    /// </summary>
    public bool IsTransitioning(PopupType popupType)
    {
        if (!TryGetPopup(
                popupType,
                out PopupBase popup))
        {
            return false;
        }

        return popup.IsTransitioning;
    }

    /// <summary>
    /// 指定したPopup本体を取得する。
    ///
    /// PauseManagerなどが、
    /// PausePopupUIを取得したい場合に使用できる。
    /// </summary>
    public T GetPopup<T>(
        PopupType popupType)
        where T : PopupBase
    {
        if (!TryGetPopup(
                popupType,
                out PopupBase popup))
        {
            return null;
        }

        T typedPopup = popup as T;

        if (typedPopup == null)
        {
            Debug.LogError(
                $"{popupType}は{typeof(T).Name}ではありません。",
                popup
            );
        }

        return typedPopup;
    }

    /// <summary>
    /// 現在最前面にあるPopupTypeを取得する。
    /// </summary>
    public bool TryGetTopPopupType(
        out PopupType popupType)
    {
        RemoveInvalidStackEntries();

        if (openedPopupStack.Count == 0)
        {
            popupType = default;
            return false;
        }

        popupType =
            openedPopupStack[openedPopupStack.Count - 1];

        return true;
    }

    /// <summary>
    /// PopupTypeに対応するPopupを取得する。
    /// </summary>
    private bool TryGetPopup(
        PopupType popupType,
        out PopupBase popup)
    {
        if (!popupDictionary.TryGetValue(
                popupType,
                out popup))
        {
            Debug.LogError(
                $"PopupManager：{popupType}が登録されていません。",
                this
            );

            return false;
        }

        if (popup == null)
        {
            Debug.LogError(
                $"PopupManager：{popupType}の参照がnullです。",
                this
            );

            return false;
        }

        return true;
    }

    /// <summary>
    /// Popupを最前面へ移動し、
    /// Stackの一番最後へ登録する。
    /// </summary>
    private void MoveToFront(
        PopupType popupType,
        PopupBase popup)
    {
        /*
         * Hierarchy上で最後のSiblingにすることで、
         * UIとして一番手前に表示する。
         */
        popup.transform.SetAsLastSibling();

        /*
         * すでにStackにある場合は一度削除し、
         * 最後へ追加し直す。
         */
        RemoveFromStack(popupType);

        openedPopupStack.Add(popupType);
    }

    /// <summary>
    /// 指定したPopupをStackから削除する。
    /// </summary>
    private void RemoveFromStack(
        PopupType popupType)
    {
        openedPopupStack.RemoveAll(
            type => type == popupType
        );
    }

    /// <summary>
    /// 閉じているPopupや参照切れをStackから除去する。
    /// </summary>
    private void RemoveInvalidStackEntries()
    {
        openedPopupStack.RemoveAll(type =>
        {
            if (!popupDictionary.TryGetValue(
                    type,
                    out PopupBase popup))
            {
                return true;
            }

            if (popup == null)
            {
                return true;
            }

            return !popup.IsOpen &&
                   !popup.IsTransitioning;
        });
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        popupDictionary.Clear();
        openedPopupStack.Clear();
    }
}