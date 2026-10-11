using System;
using System.Collections.Generic;

// 실제 전송 구현이 사용할 응답 형태와 Shop 기능 모델 사이의 변환 경계다.
public class ShopPurchaseResponse
{
    public IReadOnlyList<ShopPurchaseRewardResponse> Rewards { get; }
    public ShopPurchaseCurrenciesResponse Currencies { get; }

    public ShopPurchaseResponse(IReadOnlyList<ShopPurchaseRewardResponse> rewards,
        ShopPurchaseCurrenciesResponse currencies)
    {
        Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        Currencies = currencies ?? throw new ArgumentNullException(nameof(currencies));
    }

    // 응답의 전체 보상과 서버가 확정한 재화를 기능 모델로 보존한다.
    public PurchaseResult ToPurchaseResult()
    {
        var rewards = new List<PurchaseRewardResult>(Rewards.Count);
        foreach (ShopPurchaseRewardResponse reward in Rewards)
        {
            rewards.Add(reward.ToPurchaseRewardResult());
        }

        return new PurchaseResult(rewards, Currencies.ToUserCurrencies());
    }
}
