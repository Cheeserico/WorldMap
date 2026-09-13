using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 国IDから、正解フィードバックに表示する
/// キャラクター画像を取得するデータベース。
/// </summary>
[CreateAssetMenu(
    fileName = "CountryCharacterDatabase",
    menuName = "World Map/Country Character Database"
)]
public class CountryCharacterDatabase : ScriptableObject
{
    /// <summary>
    /// 同じキャラクターを使用する国のグループ。
    /// </summary>
    [Serializable]
    private class CharacterGroup
    {
        [Tooltip("Inspector上で確認するためのグループ名")]
        public string groupName;

        [Tooltip("この地域で表示するキャラクター画像")]
        public Sprite characterSprite;

        [Tooltip("正解時にキャラクターから飛び出す地域アイコン")]
        public Sprite[] effectIcons =
    new Sprite[3];

        [Tooltip("このキャラクターを使用する3文字の国ID")]
        public List<string> countryIds =
            new List<string>();
    }


    /// <summary>
    /// 特定の国だけ、地域画像とは別の画像へ変更する設定。
    /// </summary>
    [Serializable]
    private class CountryOverride
    {
        [Tooltip("3文字の国ID。例：JPN")]
        public string countryId;

        [Tooltip("この国だけで使用するキャラクター画像")]
        public Sprite characterSprite;
    }


    [Header("どの地域にも登録されていない国")]

    [Tooltip("割り当てがない場合に使用する汎用キャラクター")]
    [SerializeField]
    private Sprite defaultCharacterSprite;

    [Tooltip("地域未登録時に使用する汎用アイコン")]
    [SerializeField]
    private Sprite[] defaultEffectIcons =
    new Sprite[3];

    [Header("地域ごとのキャラクター")]

    [SerializeField]
    private List<CharacterGroup> characterGroups =
        new List<CharacterGroup>();


    [Header("国ごとの個別設定")]

    [Tooltip(
        "将来、特定の国だけ専用キャラクターにしたい場合に使用"
    )]
    [SerializeField]
    private List<CountryOverride> countryOverrides =
        new List<CountryOverride>();


    private Dictionary<string, Sprite>
        characterLookup;

    private Dictionary<string, Sprite[]>
    effectIconLookup;


    private void OnEnable()
    {
        BuildLookup();
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildLookup();
    }
#endif


    /// <summary>
    /// 国IDに対応するキャラクター画像を取得する。
    /// </summary>
    public Sprite GetCharacterSprite(
        string countryId)
    {
        if (string.IsNullOrWhiteSpace(countryId))
        {
            return defaultCharacterSprite;
        }

        if (characterLookup == null)
        {
            BuildLookup();
        }

        string normalizedId =
            NormalizeCountryId(countryId);

        if (characterLookup.TryGetValue(
                normalizedId,
                out Sprite characterSprite
            ))
        {
            return characterSprite;
        }

        return defaultCharacterSprite;
    }

    /// <summary>
    /// 国IDに対応する地域アイコンを取得する。
    /// </summary>
    public Sprite[] GetEffectIcons(
        string countryId)
    {
        if (string.IsNullOrWhiteSpace(countryId))
        {
            return defaultEffectIcons;
        }

        if (effectIconLookup == null)
        {
            BuildLookup();
        }

        string normalizedId =
            NormalizeCountryId(countryId);

        if (effectIconLookup.TryGetValue(
                normalizedId,
                out Sprite[] effectIcons
            ))
        {
            return effectIcons;
        }

        return defaultEffectIcons;
    }


    /// <summary>
    /// Inspectorの設定から検索用Dictionaryを作る。
    /// </summary>
    private void BuildLookup()
    {
        characterLookup =
            new Dictionary<string, Sprite>();

        effectIconLookup =
            new Dictionary<string, Sprite[]>();

        // 国専用画像を最優先で登録
        foreach (CountryOverride countryOverride
                 in countryOverrides)
        {
            if (countryOverride == null ||
                countryOverride.characterSprite == null ||
                string.IsNullOrWhiteSpace(
                    countryOverride.countryId
                ))
            {
                continue;
            }

            string countryId =
                NormalizeCountryId(
                    countryOverride.countryId
                );

            characterLookup[countryId] =
                countryOverride.characterSprite;
        }

        // 地域キャラクターと地域アイコンを登録
        foreach (CharacterGroup group
                 in characterGroups)
        {
            if (group == null ||
                group.countryIds == null)
            {
                continue;
            }

            foreach (string id in group.countryIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                string countryId =
                    NormalizeCountryId(id);

                // 国専用キャラクターがない場合のみ
                // 地域キャラクターを登録する
                if (group.characterSprite != null &&
                    !characterLookup.ContainsKey(countryId))
                {
                    characterLookup.Add(
                        countryId,
                        group.characterSprite
                    );
                }

                // 地域アイコンを登録する
                if (group.effectIcons != null &&
                    !effectIconLookup.ContainsKey(countryId))
                {
                    effectIconLookup.Add(
                        countryId,
                        group.effectIcons
                    );
                }
            }
        }
    }

    /// <summary>
    /// 国IDを大文字3文字へ統一する。
    /// </summary>
    private string NormalizeCountryId(
        string countryId)
    {
        return countryId
            .Trim()
            .ToUpperInvariant();
    }


#if UNITY_EDITOR

    /// <summary>
    /// 195か国を14種類のキャラクターへ自動で割り振る。
    /// キャラクター画像は変更せず、Country Idsだけを更新する。
    /// </summary>
    [ContextMenu(
        "Country IDs/Fill Default 195 Country Preset"
    )]
    private void FillDefaultCountryPreset()
    {
        Dictionary<string, string[]> presets =
            new Dictionary<string, string[]>
            {
                {
                    "Japan",
                    new[]
                    {
                        "JPN"
                    }
                },

                {
                    "Arab_WestAsia",
                    new[]
                    {
                        "ARE", "ARM", "AZE", "BHR",
                        "CYP", "DZA", "EGY", "GEO",
                        "IRN", "IRQ", "ISR", "JOR",
                        "KWT", "LBN", "LBY", "MAR",
                        "MRT", "OMN", "PSE", "QAT",
                        "SAU", "SDN", "SYR", "TUN",
                        "TUR", "YEM"
                    }
                },

                {
                    "SouthAsia",
                    new[]
                    {
                        "AFG", "BGD", "BTN", "IND",
                        "MDV", "NPL", "PAK", "LKA"
                    }
                },

                {
                    "SoutheastAsia",
                    new[]
                    {
                        "BRN", "KHM", "IDN", "LAO",
                        "MYS", "MMR", "PHL", "SGP",
                        "THA", "TLS", "VNM"
                    }
                },

                {
                    "Africa",
                    new[]
                    {
                        "AGO", "BEN", "BWA", "BFA",
                        "BDI", "CPV", "CMR", "CAF",
                        "TCD", "COM", "COG", "COD",
                        "CIV", "DJI", "GNQ", "ERI",
                        "SWZ", "ETH", "GAB", "GMB",
                        "GHA", "GIN", "GNB", "KEN",
                        "LSO", "LBR", "MDG", "MWI",
                        "MLI", "MUS", "MOZ", "NAM",
                        "NER", "NGA", "RWA", "STP",
                        "SEN", "SYC", "SLE", "SOM",
                        "ZAF", "SSD", "TZA", "TGO",
                        "UGA", "ZMB", "ZWE"
                    }
                },

                {
                    "EasternEurope",
                    new[]
                    {
                        "ALB", "BLR", "BIH", "BGR",
                        "HRV", "CZE", "EST", "HUN",
                        "LVA", "LTU", "MDA", "MNE",
                        "MKD", "POL", "ROU", "RUS",
                        "SRB", "SVK", "SVN", "UKR"
                    }
                },

                {
                    "Western_CentralEurope",
                    new[]
                    {
                        "AUT", "BEL", "FRA", "DEU",
                        "IRL", "LIE", "LUX", "NLD",
                        "CHE", "GBR"
                    }
                },

                {
                    "SouthernEurope",
                    new[]
                    {
                        "AND", "GRC", "ITA",
                        "MLT", "MCO", "PRT",
                        "SMR", "ESP", "VAT"
                    }
                },

                {
                    "Pacific_Oceania",
                    new[]
                    {
                        "AUS", "FJI", "KIR", "MHL",
                        "FSM", "NRU", "NZL", "PLW",
                        "PNG", "WSM", "SLB", "TON",
                        "TUV", "VUT"
                    }
                },

                {
                    "CentralAsia",
                    new[]
                    {
                        "KAZ", "KGZ", "TJK",
                        "TKM", "UZB"
                    }
                },

                {
                    "LatinAmerica",
                    new[]
                    {
                        "ATG", "ARG", "BHS", "BRB",
                        "BLZ", "BOL", "BRA", "CHL",
                        "COL", "CRI", "CUB", "DMA",
                        "DOM", "ECU", "SLV", "GRD",
                        "GTM", "GUY", "HTI", "HND",
                        "JAM", "MEX", "NIC", "PAN",
                        "PRY", "PER", "KNA", "LCA",
                        "VCT", "SUR", "TTO", "URY",
                        "VEN"
                    }
                },

                {
                    "NorthernEurope",
                    new[]
                    {
                        "DNK", "FIN", "ISL",
                        "NOR", "SWE"
                    }
                },

                {
                    "China_EastAsia",
                    new[]
                    {
                        "CHN", "KOR", "PRK", "MNG"
                    }
                },

                {
                    "NorthAmerica",
                    new[]
                    {
                        "CAN", "USA"
                    }
                }
            };

        int updatedGroupCount = 0;
        int assignedCountryCount = 0;

        foreach (CharacterGroup group
                 in characterGroups)
        {
            if (group == null ||
                string.IsNullOrWhiteSpace(
                    group.groupName
                ))
            {
                continue;
            }

            string groupName =
                group.groupName.Trim();

            if (!presets.TryGetValue(
                    groupName,
                    out string[] countryIds
                ))
            {
                Debug.LogWarning(
                    $"CountryCharacterDatabase：" +
                    $"プリセットに存在しないGroup Nameです。" +
                    $"「{groupName}」"
                );

                continue;
            }

            group.countryIds =
                new List<string>(
                    countryIds
                );

            updatedGroupCount++;
            assignedCountryCount +=
                countryIds.Length;
        }

        BuildLookup();

        UnityEditor.EditorUtility.SetDirty(
            this
        );

        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log(
            $"CountryCharacterDatabase：" +
            $"{updatedGroupCount}グループへ" +
            $"{assignedCountryCount}か国を登録しました。"
        );
    }

#endif

}