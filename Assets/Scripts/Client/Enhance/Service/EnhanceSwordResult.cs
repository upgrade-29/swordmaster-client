using Newtonsoft.Json;

public class EnhanceSwordResult
{
    public readonly EnhanceSwordOutcome result;
    public readonly UserSword sword;
    public readonly UserCurrencies currencies;
    public readonly CombatStat stats;
    public readonly long combatPower;

    [JsonConstructor]
    public EnhanceSwordResult(EnhanceSwordOutcome result, UserSword sword, UserCurrencies currencies,
        CombatStat stats, long combatPower)
    {
        this.result = result;
        this.sword = sword;
        this.currencies = currencies;
        this.stats = stats;
        this.combatPower = combatPower;
    }
}
