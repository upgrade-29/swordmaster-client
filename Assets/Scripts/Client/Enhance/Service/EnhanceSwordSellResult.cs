using System.Collections.Generic;
using Newtonsoft.Json;

public class EnhanceSwordSellResult
{
    public readonly UserSword sword;
    public readonly IReadOnlyList<PurchaseRewardResult> rewards;
    public readonly UserCurrencies currencies;
    public readonly CombatStat stats;
    public readonly long combatPower;

    [JsonConstructor]
    public EnhanceSwordSellResult(UserSword sword, IReadOnlyList<PurchaseRewardResult> rewards, UserCurrencies currencies,
        CombatStat stats, long combatPower)
    {
        this.sword = sword;
        this.rewards = rewards ?? new List<PurchaseRewardResult>();
        this.currencies = currencies;
        this.stats = stats;
        this.combatPower = combatPower;
    }
}
