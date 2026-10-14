using System;
using Newtonsoft.Json;

// 서버가 실제로 지급한 보상 1건. 상점 구매와 검 판매 응답이 같이 쓴다
public class RewardResponse
{
    public readonly RewardType rewardType;
    public readonly string rewardCode; // GOLD는 null
    public readonly long amount;

    [JsonConstructor]
    public RewardResponse(RewardType rewardType, string rewardCode, long amount)
    {
        if (rewardCode == null && rewardType != RewardType.Gold)
        {
            throw new ArgumentException("GOLD 이외 보상의 rewardCode는 null일 수 없습니다.", nameof(rewardCode));
        }

        this.rewardType = rewardType;
        this.rewardCode = rewardCode;
        this.amount = amount;
    }
}
