using System.Collections.Generic;
using Newtonsoft.Json;

public class EnhanceSwordSellResult
{
    public readonly UserSword sword;
    public readonly IReadOnlyList<PurchaseRewardResult> rewards;
    public readonly UserCurrencies currencies;

    [JsonConstructor]
    public EnhanceSwordSellResult(UserSword sword, IReadOnlyList<PurchaseRewardResult> rewards, UserCurrencies currencies)
    {
        this.sword = sword;
        this.rewards = rewards ?? new List<PurchaseRewardResult>();
        this.currencies = currencies;
    }
}
