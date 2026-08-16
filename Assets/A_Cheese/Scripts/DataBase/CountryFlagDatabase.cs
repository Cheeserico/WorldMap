using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CountryIdから国旗Spriteを取得する。
///
/// 国旗画像は
/// Assets/Resources/Flags
/// に配置し、
///
/// JPN.png
/// USA.png
/// FRA.png
///
/// のようにCountryIdと同じ名前にする。
/// </summary>
public class CountryFlagDatabase : MonoBehaviour
{
    private Dictionary<string, Sprite> flagDictionary =
        new Dictionary<string, Sprite>();


    private void Awake()
    {
        LoadFlags();
    }


    /// <summary>
    /// Resources/Flags内の国旗をすべて読み込む。
    /// </summary>
    private void LoadFlags()
    {
        flagDictionary.Clear();

        Sprite[] flags =
            Resources.LoadAll<Sprite>("Flags");

        foreach (Sprite flag in flags)
        {
            string countryId =
                flag.name.ToUpper();

            if (flagDictionary.ContainsKey(countryId))
            {
                Debug.LogWarning(
                    $"CountryFlagDatabase：重複した国旗があります。ID = {countryId}"
                );

                continue;
            }

            flagDictionary.Add(
                countryId,
                flag
            );
        }

        Debug.Log(
            $"CountryFlagDatabase：{flagDictionary.Count}個の国旗を読み込みました。"
        );
    }


    /// <summary>
    /// CountryIdから国旗を取得する。
    /// </summary>
    public Sprite GetFlag(string countryId)
    {
        if (string.IsNullOrEmpty(countryId))
        {
            return null;
        }

        string key =
            countryId.ToUpper();

        if (flagDictionary.TryGetValue(
            key,
            out Sprite flag))
        {
            return flag;
        }

        Debug.LogWarning(
            $"CountryFlagDatabase：国旗が見つかりません。ID = {countryId}"
        );

        return null;
    }
}