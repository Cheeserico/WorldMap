using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public enum AdAudienceMode
{
    General = 0,
    ChildSafe = 1
}

/// <summary>
/// 広告の年齢方針を管理する。
/// SDK初期化・同意画面表示はAdManagerが担当する。
/// ストアの対象年齢設定やポリシー対応全体を変更するものではない。
/// </summary>
[DisallowMultipleComponent]
public sealed class AdAudienceSettings : MonoBehaviour
{
    [Header("起動時の広告方針（再生前に設定）")]
    [Tooltip("General: 一般向け。ChildSafe: 全員に子ども向け広告制限を適用。")]
    [SerializeField] private AdAudienceMode audienceMode = AdAudienceMode.General;


    private bool isLocked;
    private AdAudienceMode lockedMode;
    private bool lockedUnderAgeOfConsent;

    public AdAudienceMode CurrentMode => isLocked ? lockedMode : audienceMode;
    public bool IsLocked => isLocked;

    /// <summary>同意取得開始前だけ変更可能。再生中のInspector変更は使用しない。</summary>
    public bool SetAudienceMode(AdAudienceMode mode)
    {
        if (!Enum.IsDefined(typeof(AdAudienceMode), mode))
            throw new ArgumentOutOfRangeException(nameof(mode));

        if (isLocked)
        {
            Debug.LogWarning("広告方針は同意取得開始後に変更できません。次回起動時に変更してください。", this);
            return false;
        }

        audienceMode = mode;
        return true;
    }

    /// <summary>
    /// UMPへの最初のリクエスト前に呼ぶ。
    /// Generalでも利用者の同意可能年齢の扱いは別に判断して渡す。
    /// falseは成人確認済み・同意済みという意味ではない。
    /// ChildSafeでは全員に同意可能年齢未満の扱いを適用する。
    /// </summary>
    public ConsentRequestParameters PrepareForConsent(bool userUnderAgeOfConsent)
    {
        if (!Enum.IsDefined(typeof(AdAudienceMode), audienceMode))
            throw new InvalidOperationException("広告方針の設定が不正です。");

        if (!isLocked)
        {
            lockedMode = audienceMode;
            lockedUnderAgeOfConsent =
                lockedMode == AdAudienceMode.ChildSafe || userUnderAgeOfConsent;

            // 他の広告設定を保持する。
            RequestConfiguration configuration = MobileAds.GetRequestConfiguration();
            if (configuration == null)
                configuration = new RequestConfiguration();

            configuration.AgeRestrictedTreatment = lockedUnderAgeOfConsent
                ? AgeRestrictedTreatment.Child
                : AgeRestrictedTreatment.Unspecified;

            // G設定だけで子ども向け対応全体が完了するわけではない。
            configuration.MaxAdContentRating = MaxAdContentRating.G;
            MobileAds.SetRequestConfiguration(configuration);
            isLocked = true;
        }
        else if (lockedMode == AdAudienceMode.General &&
                 userUnderAgeOfConsent != lockedUnderAgeOfConsent)
        {
            throw new InvalidOperationException("同意取得開始後に年齢の扱いは変更できません。");
        }

        var parameters = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = lockedUnderAgeOfConsent
        };

        return parameters;
    }

}
