using Newtonsoft.Json;

public class ShopProductRewardData
{
    public readonly string productCode;
    public readonly RewardType rewardType;
    public readonly string rewardCode;
    public readonly long amount;

    [JsonConstructor]
    public ShopProductRewardData(string productCode, RewardType rewardType, string rewardCode, long amount)
    {
        this.productCode = productCode;
        this.rewardType = rewardType;
        this.rewardCode = rewardCode;
        this.amount = amount;
    }
}
