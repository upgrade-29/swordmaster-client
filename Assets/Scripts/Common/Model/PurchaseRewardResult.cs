using Newtonsoft.Json;

// 구매 완료 응답에 포함되어 실제로 지급된 보상 1건
public class PurchaseRewardResult
{
    public readonly RewardType rewardType;
    public readonly string rewardCode;
    public readonly long amount;

    [JsonConstructor]
    public PurchaseRewardResult(RewardType rewardType, string rewardCode, long amount)
    {
        this.rewardType = rewardType;
        this.rewardCode = rewardCode;
        this.amount = amount;
    }
}
