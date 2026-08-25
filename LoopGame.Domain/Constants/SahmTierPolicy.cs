namespace LoopGame.Domain.Constants;

/// <summary>
/// Pure domain policy mapping Sahm tier shop items (category 'sahm_tier') to
/// subscription data. Shop items follow the item_key convention
/// 'sahm_' + lower-case tier name, e.g. 'sahm_pro' (see UC-ECO-09 / SD-SAHM-02).
/// </summary>
public static class SahmTierPolicy
{
    /// <summary>Shop item_key prefix for Sahm subscription items.</summary>
    public const string ItemKeyPrefix = "sahm_";

    /// <summary>
    /// Parses a shop item_key ('sahm_pro') into its target tier.
    /// Returns false for keys that are not valid Sahm tier items.
    /// </summary>
    public static bool TryParseFromItemKey(string? itemKey, out SahmTier tier)
    {
        tier = SahmTier.Free;
        if (itemKey is null || !itemKey.StartsWith(ItemKeyPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var tierName = itemKey[ItemKeyPrefix.Length..];
        return Enum.TryParse(tierName, ignoreCase: true, out tier);
    }

    /// <summary>
    /// Daily hint limit stored on a new SahmSubscription row.
    /// NOTE: HintLimits.Enterprise is int.MaxValue ("unlimited"), but the
    /// daily_hint_limit column is smallint/byte — 255 is the documented
    /// "unlimited" sentinel (SD-SAHM-01 sequence diagram).
    /// </summary>
    public static byte GetDailyHintLimit(SahmTier tier) => tier switch
    {
        SahmTier.Free       => HintLimits.Free,
        SahmTier.Pro        => HintLimits.Pro,
        SahmTier.Team       => HintLimits.Team,
        SahmTier.Enterprise => byte.MaxValue,
        _ => HintLimits.Free
    };
}
