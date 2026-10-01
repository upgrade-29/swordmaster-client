using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class UserCurrencies
{
    [JsonProperty("gold")] private long gold;
    [JsonProperty("diamond")] private long diamond;

    public long Gold => gold;
    public long Diamond => diamond;

    [JsonConstructor]
    public UserCurrencies(long gold, long diamond)
    {
        this.gold = gold;
        this.diamond = diamond;
    }

    public long Get(CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Gold: return gold;
            case CurrencyType.Diamond: return diamond;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    // amount가 음수이면 차감합니다.
    public void Add(CurrencyType type, long amount)
    {
        var result = Get(type) + amount;
        if (result < 0)
            throw new InvalidOperationException($"{type} would be negative: {result}");

        switch (type)
        {
            case CurrencyType.Gold: gold = result; break;
            case CurrencyType.Diamond: diamond = result; break;
        }
    }

    // 서버가 확정한 최종 값(구매 응답 등)으로 재화를 그대로 덮어쓴다. 클라이언트가 델타를 계산해 유추하지 않는다.
    public void SyncFrom(UserCurrencies latest)
    {
        gold = latest.gold;
        diamond = latest.diamond;
    }
}
