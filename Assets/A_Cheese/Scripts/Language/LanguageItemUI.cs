using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 言語一覧の1項目。
/// </summary>
public class LanguageItemUI : MonoBehaviour
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private TMP_Text languageNameText;

    [SerializeField]
    private GameObject selectedMark;

    public void Setup(
        string languageName,
        bool isSelected,
        Action onClicked
    )
    {
        if (languageNameText != null)
        {
            languageNameText.text =
                languageName;
        }

        SetSelected(isSelected);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                () => onClicked?.Invoke()
            );
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null)
        {
            selectedMark.SetActive(selected);
        }
    }
}