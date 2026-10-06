using Newtonsoft.Json;

public class EnhanceSwordResult
{
    public readonly EnhanceSwordOutcome result;
    public readonly UserSword sword;
    public readonly UserCurrencies currencies;

    [JsonConstructor]
    public EnhanceSwordResult(EnhanceSwordOutcome result, UserSword sword, UserCurrencies currencies)
    {
        this.result = result;
        this.sword = sword;
        this.currencies = currencies;
    }
}
