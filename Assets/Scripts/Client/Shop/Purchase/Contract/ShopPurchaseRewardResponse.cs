using System;

// 서버 구매 응답의 보상 한 건을 전송 형식으로 보관한다.
public class ShopPurchaseRewardResponse
{
    public RewardType RewardType { get; }
    public string RewardCode { get; }
    public long Amount { get; }

    public ShopPurchaseRewardResponse(RewardType rewardType, string rewardCode, long amount)
    {
        if (rewardCode == null && rewardType != RewardType.Gold)
        {
            throw new ArgumentException("GOLD 이외 보상의 rewardCode는 null일 수 없습니다.", nameof(rewardCode));
        }

        RewardType = rewardType;
        RewardCode = rewardCode;
        Amount = amount;
    }

    // 서버 보상 1건을 화면과 상태가 사용하는 결과 모델로 변환한다.
    public PurchaseRewardResult ToPurchaseRewardResult()
    {
        return new PurchaseRewardResult(RewardType, RewardCode, Amount);
    }
}
